#!/usr/bin/env sh
# Rebuilds the checked-in fixtures ComponentModBackendTests loads:
#   ../component_guest.wasm                 the fixture (world `fixture`)
#   ../component_guest_missing_export.wasm  `setup` declares `tick` but exports none
set -e
cd "$(dirname "$0")"
# The contract the worlds `use` / `include`, resolved from wit/deps/.
mkdir -p wit/deps/tinyecs-mod
cp ../../../../src/TinyEcs.Bevy.Modding/abi/tinyecs-mod.wit wit/deps/tinyecs-mod/
cargo build --release --target wasm32-wasip2
cp target/wasm32-wasip2/release/component_guest.wasm ../component_guest.wasm
cargo build --release --target wasm32-wasip2 --features missing-export
cp target/wasm32-wasip2/release/component_guest.wasm ../component_guest_missing_export.wasm
