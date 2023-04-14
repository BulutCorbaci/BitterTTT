#!/bin/sh
echo -ne '\033c\033]0;Bitter TTT\a'
base_path="$(dirname "$(realpath "$0")")"
"$base_path/Bitter TTT Trainer.x86_64" "$@"
