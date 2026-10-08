// ComponentModBackend test fixture: a tinyecs:modding `guest` component exercising every
// system parameter kind (commands, query terms, res / res-mut, events) plus an
// on-add observer and two on-packet observers. ComponentModBackendTests asserts the effects host-side.

wit_bindgen::generate!({ world: "guest", path: "../../../../src/TinyEcs.Bevy.Modding/abi/tinyecs-mod.wit" });

use tinyecs::modding::ecs::{PacketFilter, Schedule, System, Term, Trigger};

struct Fixture;

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

        let added = System::new("count_added");
        added.add_query(&[Term::Added("test/pos".into())]);
        added.add_res_mut("test/added");
        added.after(&tick);

        app.add_systems(Schedule::Update, &[&tick, &added]);

        let on_tag = System::new("on_tag");
        on_tag.add_commands();
        app.add_observer(&Trigger::OnAdd("test/tag".into()), &on_tag);

        let on_in = System::new("on_in");
        on_in.add_res_mut("test/score");
        app.add_observer(
            &Trigger::OnPacket(PacketFilter { direction: PacketDirection::Incoming, ids: vec![0x10] }),
            &on_in,
        );

        let on_out = System::new("on_out");
        app.add_observer(
            &Trigger::OnPacket(PacketFilter { direction: PacketDirection::Outgoing, ids: vec![] }),
            &on_out,
        );
    }

    fn run(system: String, params: Vec<Param>) {
        match system.as_str() {
            "tick" => {
                let [Param::Commands(cmds), Param::Query(q), Param::Res(score), Param::Events(pings)] =
                    &params[..]
                else {
                    panic!("tick: unexpected params")
                };

                while let Some(row) = q.next() {
                    let pos = row.get(0);
                    let vel = row.get(1);
                    let x = num(&pos, "X") + num(&vel, "X");
                    let y = num(&pos, "Y") + num(&vel, "Y");
                    row.set(0, &format!("{{\"X\":{},\"Y\":{}}}", x, y));
                    if x >= 3 {
                        cmds.insert(row.entity(), &[("test/tag".into(), "{}".into())]);
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
            "count_added" => {
                let [Param::Query(q), Param::Res(added)] = &params[..] else {
                    panic!("count_added: unexpected params")
                };
                let mut n = 0;
                while q.next().is_some() {
                    n += 1;
                }
                let total = added.get().map(|s| num(&s, "Value")).unwrap_or(0) + n;
                added.set(&format!("{{\"Value\":{}}}", total));
            }
            other => panic!("unknown system {other}"),
        }
    }

    fn observe(system: String, trigger: TriggerData, params: Vec<Param>) {
        assert_eq!(system, "on_tag");
        let [Param::Commands(cmds)] = &params[..] else {
            panic!("on_tag: unexpected params")
        };
        cmds.insert(
            trigger.entity,
            &[("test/seen".into(), format!("{{\"Value\":{}}}", trigger.entity))],
        );
    }

    // on_in (incoming 0x10): adds 1000 to the score, blocks [0x10, 0], else appends 0x42.
    // on_out (every outgoing id): replaces with [0x99].
    fn observe_packet(system: String, direction: PacketDirection, packet: Vec<u8>, params: Vec<Param>) -> Verdict {
        match system.as_str() {
            "on_in" => {
                assert!(matches!(direction, PacketDirection::Incoming));
                let [Param::Res(score)] = &params[..] else {
                    panic!("on_in: unexpected params")
                };
                let value = score.get().map(|s| num(&s, "Value")).unwrap_or(0) + 1000;
                score.set(&format!("{{\"Value\":{}}}", value));
                if packet.get(1) == Some(&0) {
                    return Verdict::Block;
                }
                let mut out = packet;
                out.push(0x42);
                Verdict::Replace(out)
            }
            "on_out" => {
                assert!(matches!(direction, PacketDirection::Outgoing));
                Verdict::Replace(vec![0x99])
            }
            other => panic!("unknown packet observer {other}"),
        }
    }
}

export!(Fixture);
