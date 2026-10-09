// ComponentModBackend test fixture: a tinyecs:modding component exercising every
// system parameter kind (commands, query terms, res / res-mut, events) plus an
// on-add observer and two on-packet observers, and two run-on-change systems (`watch`,
// `beat`) using res.unchanged / commands.set-resource. Every system is its own export
// (wit/world.wit), called by the host with typed params. ComponentModBackendTests
// asserts the effects host-side. ../component_guest_v0.wasm is a guest built before
// per-system exports (the generic `run` dispatcher): it must fail to load, loudly.

use std::sync::atomic::{AtomicI64, Ordering};

wit_bindgen::generate!({
    world: "test:fixture/fixture",
    path: "wit",
    generate_all,
});

// commands / query / res / events / trigger-data / packet-direction / verdict come in through
// the world's `use`.
use tinyecs::modding::ecs::{PacketFilter, Schedule, System, Term, Trigger};

struct Fixture;

static WATCH_RUNS: AtomicI64 = AtomicI64::new(0);
static BEATS: AtomicI64 = AtomicI64::new(0);

// Reads the integer after `"key":` — enough JSON for the fixture's flat payloads.
fn num(json: &str, key: &str) -> i64 {
    let pat = format!("\"{}\":", key);
    let Some(at) = json.find(&pat) else { return 0 };
    let rest = json[at + pat.len()..].trim_start();
    let end = rest
        .find(|c: char| !(c == '-' || c.is_ascii_digit()))
        .unwrap_or(rest.len());
    rest[..end].parse().unwrap_or(0)
}

impl Guest for Fixture {
    fn setup(app: App) {
        let tick = System::new("tick");
        tick.add_commands();
        tick.add_query(&[
            Term::Mut("test/pos".into()),
            Term::Ref("test/vel".into()),
            Term::Without("test/frozen".into()),
        ]);
        tick.add_res_mut("test/score");
        tick.add_events("test/ping");

        let added = System::new("count-added");
        added.add_query(&[Term::Added("test/pos".into())]);
        added.add_res_mut("test/added");
        added.after(&tick);

        app.add_systems(Schedule::Update, &[&tick, &added]);

        let watch = System::new("watch");
        watch.add_commands();
        watch.add_query(&[Term::Changed("test/watched".into())]);
        watch.add_res("test/knob");
        watch.add_events("test/poke");
        watch.run_on_change();

        // Commands only: no input parameter, so it never skips.
        let beat = System::new("beat");
        beat.add_commands();
        beat.run_on_change();

        app.add_systems(Schedule::PostUpdate, &[&watch, &beat]);

        let on_tag = System::new("on-tag");
        on_tag.add_commands();
        app.add_observer(&Trigger::OnAdd("test/tag".into()), &on_tag);

        let on_in = System::new("on-in");
        on_in.add_res_mut("test/score");
        app.add_observer(
            &Trigger::OnPacket(PacketFilter { direction: PacketDirection::Incoming, ids: vec![0x10] }),
            &on_in,
        );

        let on_out = System::new("on-out");
        app.add_observer(
            &Trigger::OnPacket(PacketFilter { direction: PacketDirection::Outgoing, ids: vec![] }),
            &on_out,
        );
    }

    fn tick(cmds: Commands, movers: Query, score: Res, pings: Events) {
        for row in movers.rows() {
            let (pos, vel) = (&row.values[0], &row.values[1]);
            let x = num(pos, "X") + num(vel, "X");
            let y = num(pos, "Y") + num(vel, "Y");
            movers.set(row.entity, 0, &format!("{{\"X\":{},\"Y\":{}}}", x, y));
            if x >= 3 {
                cmds.insert(row.entity, &[("test/tag".into(), "{}".into())]);
            }
        }

        let seen = pings.read();
        let mut value = score.get().map(|s| num(&s, "Value")).unwrap_or(0);
        value += 1 + 100 * seen.iter().map(|p| num(p, "N")).sum::<i64>();
        score.set(&format!("{{\"Value\":{}}}", value));

        if value == 1 {
            let e = cmds.spawn(&[("test/pos".into(), "{\"X\":100,\"Y\":0}".into())]);
            cmds.insert(e, &[("test/frozen".into(), "{}".into())]);
            cmds.send("test/ping", "{\"N\":1}");
        }
    }

    fn count_added(added: Query, total: Res) {
        let n = added.rows().len() as i64;
        let sum = total.get().map(|s| num(&s, "Value")).unwrap_or(0) + n;
        total.set(&format!("{{\"Value\":{}}}", sum));
    }

    // Reports each run into test/probe: how many runs so far, whether the knob was
    // unchanged since the value `get` last returned, the rows and pokes seen.
    // Poke 13 / 14 set a read-only / unknown resource (both trap).
    fn watch(cmds: Commands, watched: Query, knob: Res, pokes: Events) {
        let runs = WATCH_RUNS.fetch_add(1, Ordering::Relaxed) + 1;
        let unchanged = knob.unchanged();
        let knob_value = if unchanged { -1 } else { knob.get().map(|s| num(&s, "Value")).unwrap_or(0) };
        let rows = watched.rows().len();
        let pokes = pokes.read();
        for p in &pokes {
            match num(p, "N") {
                13 => cmds.set_resource("test/clock", "{\"Value\":1}"),
                14 => cmds.set_resource("test/nope", "{}"),
                _ => {}
            }
        }
        cmds.set_resource(
            "test/probe",
            &format!(
                "{{\"Runs\":{},\"Unchanged\":{},\"Knob\":{},\"Rows\":{},\"Pokes\":{}}}",
                runs,
                unchanged as i64,
                knob_value,
                rows,
                pokes.len()
            ),
        );
    }

    fn beat(cmds: Commands) {
        let n = BEATS.fetch_add(1, Ordering::Relaxed) + 1;
        cmds.set_resource("test/beats", &format!("{{\"Value\":{}}}", n));
    }

    fn on_tag(trigger: TriggerData, cmds: Commands) {
        cmds.insert(
            trigger.entity,
            &[("test/seen".into(), format!("{{\"Value\":{}}}", trigger.entity))],
        );
    }

    // Incoming 0x10: adds 1000 to the score, blocks [0x10, 0], else appends 0x42.
    fn on_in(direction: PacketDirection, packet: Vec<u8>, score: Res) -> Verdict {
        assert!(matches!(direction, PacketDirection::Incoming));
        let value = score.get().map(|s| num(&s, "Value")).unwrap_or(0) + 1000;
        score.set(&format!("{{\"Value\":{}}}", value));
        if packet.get(1) == Some(&0) {
            return Verdict::Block;
        }
        let mut out = packet;
        out.push(0x42);
        Verdict::Replace(out)
    }

    // Every outgoing id: replaces with [0x99].
    fn on_out(direction: PacketDirection, _packet: Vec<u8>) -> Verdict {
        assert!(matches!(direction, PacketDirection::Outgoing));
        Verdict::Replace(vec![0x99])
    }
}

export!(Fixture);
