#!/usr/bin/env sh
# Rebuilds ../component_guest.wasm (the checked-in fixture ComponentModBackendTests loads).
set -e
cd "$(dirname "$0")"
# The contract the world `use`s / `include`s, resolved from wit/deps/.
mkdir -p wit/deps/tinyecs-mod
cp ../../../../src/TinyEcs.Bevy.Modding/abi/tinyecs-mod.wit wit/deps/tinyecs-mod/
cargo build --release --target wasm32-wasip2
cp target/wasm32-wasip2/release/component_guest.wasm ../component_guest.wasm
