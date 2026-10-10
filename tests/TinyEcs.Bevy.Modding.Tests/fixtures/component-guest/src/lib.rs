// ComponentModBackend test fixture: a tinyecs:modding component exercising every
// system parameter kind (commands, query terms, res / res-mut, events) plus on-add
// observers (a typed `(entity, value: tag-value)` one and a bare `(entity)` one), two
// on-packet observers, and two run-on-change systems (`watch`, `beat`) using
// res.unchanged / set-resource. Every system is its own export (wit/world.wit), called by
// the host with typed params. Values cross through the test host's `values` interface as
// Encoding.Typed bytes, encoded / decoded by hand below. ComponentModBackendTests asserts
// the effects host-side.
//
// Built with `--features missing-export` it is a guest whose `setup` declares `tick`
// without exporting it: it must fail to load, naming the missing export.

#[cfg(not(feature = "missing-export"))]
mod fixture {
    use std::sync::atomic::{AtomicI64, Ordering};

    wit_bindgen::generate!({
        world: "test:fixture/fixture",
        path: "wit",
        generate_all,
    });

    use test::fixture::values;
    use tinyecs::modding::ecs::{PacketFilter, Schedule, System, Term, Trigger};

    struct Fixture;

    static WATCH_RUNS: AtomicI64 = AtomicI64::new(0);
    static BEATS: AtomicI64 = AtomicI64::new(0);

    // Encoding.Typed: little-endian, fields in declaration order.
    fn s32_at(v: &[u8], at: usize) -> i32 {
        i32::from_le_bytes(v[at..at + 4].try_into().unwrap())
    }

    fn s64(v: &[u8]) -> i64 {
        i64::from_le_bytes(v[..8].try_into().unwrap())
    }

    fn pair(x: i32, y: i32) -> Vec<u8> {
        let mut out = x.to_le_bytes().to_vec();
        out.extend_from_slice(&y.to_le_bytes());
        out
    }

    fn enc_s64(v: i64) -> Vec<u8> {
        v.to_le_bytes().to_vec()
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

            let typed = System::new("on-tag-typed");
            typed.add_commands();
            app.add_observer(&Trigger::OnAdd("test/tag".into()), &typed);
            let bare = System::new("on-tag-bare");
            bare.add_commands();
            app.add_observer(&Trigger::OnAdd("test/tag".into()), &bare);

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
            let ids = movers.entities();
            let pos = values::column(&movers, 0, "test/pos");
            let vel = values::column(&movers, 1, "test/vel");
            assert_eq!(ids.len(), pos.len(), "column disagrees with query.entities");
            for (i, &e) in ids.iter().enumerate() {
                let x = s32_at(&pos[i], 0) + s32_at(&vel[i], 0);
                let y = s32_at(&pos[i], 4) + s32_at(&vel[i], 4);
                values::set(&movers, e, 0, "test/pos", &pair(x, y));
                if x >= 3 {
                    // tag-value { v: 0 }
                    values::insert(&cmds, e, "test/tag", &0i32.to_le_bytes());
                }
            }

            let seen = values::read(&pings, "test/ping");
            assert_eq!(seen.len() as u32, pings.len(), "events.len disagrees with read");
            let mut value = values::get(&score, "test/score").map(|s| s64(&s)).unwrap_or(0);
            value += 1 + 100 * seen.iter().map(|p| s32_at(p, 0) as i64).sum::<i64>();
            values::put(&score, "test/score", &enc_s64(value));

            if value == 1 {
                let e = cmds.spawn();
                values::insert(&cmds, e, "test/pos", &pair(100, 0));
                values::insert(&cmds, e, "test/frozen", &[]); // a tag: no bytes
                values::send(&cmds, "test/ping", &1i32.to_le_bytes());
            }
        }

        fn count_added(added: Query, total: Res) {
            let ids = added.entities();
            let col = values::column(&added, 0, "test/pos");
            assert_eq!(ids.len(), col.len(), "column disagrees with query.entities");
            let sum = values::get(&total, "test/added").map(|s| s64(&s)).unwrap_or(0) + ids.len() as i64;
            values::put(&total, "test/added", &enc_s64(sum));
        }

        // Reports each run into test/probe: how many runs so far, whether the knob was
        // unchanged since the value `get` last returned, the rows and pokes seen.
        // Poke 13 / 14 set a read-only / unknown resource (both trap).
        fn watch(cmds: Commands, watched: Query, knob: Res, pokes: Events) {
            let runs = WATCH_RUNS.fetch_add(1, Ordering::Relaxed) + 1;
            let unchanged = knob.unchanged();
            let knob_value = if unchanged {
                -1
            } else {
                values::get(&knob, "test/knob").map(|s| s64(&s)).unwrap_or(0)
            };
            let rows = watched.entities().len() as i64;
            let pokes = values::read(&pokes, "test/poke");
            for p in &pokes {
                match s32_at(p, 0) {
                    13 => values::set_resource(&cmds, "test/clock", &enc_s64(1)),
                    14 => values::set_resource(&cmds, "test/nope", &[]),
                    _ => {}
                }
            }
            let mut probe = Vec::with_capacity(40);
            for v in [runs, unchanged as i64, knob_value, rows, pokes.len() as i64] {
                probe.extend_from_slice(&v.to_le_bytes());
            }
            values::set_resource(&cmds, "test/probe", &probe);
        }

        fn beat(cmds: Commands) {
            let n = BEATS.fetch_add(1, Ordering::Relaxed) + 1;
            values::set_resource(&cmds, "test/beats", &enc_s64(n));
        }

        // test/seen-typed = the tag's V as the typed record carried it.
        fn on_tag_typed(entity: u64, value: TagValue, cmds: Commands) {
            values::insert(&cmds, entity, "test/seen-typed", &enc_s64(value.v as i64));
        }

        fn on_tag_bare(entity: u64, cmds: Commands) {
            values::insert(&cmds, entity, "test/seen-bare", &enc_s64(entity as i64));
        }

        // Incoming 0x10: adds 1000 to the score, blocks [0x10, 0], else appends 0x42.
        fn on_in(direction: PacketDirection, packet: Vec<u8>, score: Res) -> Verdict {
            assert!(matches!(direction, PacketDirection::Incoming));
            let value = values::get(&score, "test/score").map(|s| s64(&s)).unwrap_or(0) + 1000;
            values::put(&score, "test/score", &enc_s64(value));
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
}

#[cfg(feature = "missing-export")]
mod missing_export {
    wit_bindgen::generate!({
        world: "test:fixture/missing-export",
        path: "wit",
        generate_all,
    });

    use tinyecs::modding::ecs::{Schedule, System};

    struct Fixture;

    impl Guest for Fixture {
        fn setup(app: App) {
            let tick = System::new("tick");
            tick.add_commands();
            app.add_systems(Schedule::Update, &[&tick]);
        }
    }

    export!(Fixture);
}
