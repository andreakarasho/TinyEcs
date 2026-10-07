#!/usr/bin/env sh
# Rebuilds ../component_guest.wasm (the checked-in fixture ComponentModBackendTests loads).
set -e
cd "$(dirname "$0")"
cargo build --release --target wasm32-wasip2
cp target/wasm32-wasip2/release/component_guest.wasm ../component_guest.wasm
