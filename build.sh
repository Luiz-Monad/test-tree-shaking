#!/usr/bin/env bash

set -eu

cd "$(dirname "$0")"

PAKET_EXE=paket
FAKE_EXE=fake

FSIARGS=""
FSIARGS2=""
OS=${OS:-"unknown"}
if [ "$OS" != "Windows_NT" ]
then
  # Can't use FSIARGS="--fsiargs -d:MONO" in zsh, so split it up
  # (Can't use arrays since dash can't handle them)
  FSIARGS="--fsiargs"
  FSIARGS2="-d:MONO"
fi

run() {
  if [ "$OS" != "Windows_NT" ]
  then
    mono "$@"
  else
    "$@"
  fi
}

[ ! -e .fake/build.fsx ] && run $PAKET_EXE install

run $PAKET_EXE restore

run $FAKE_EXE "$@" $FSIARGS $FSIARGS2 build.fsx

