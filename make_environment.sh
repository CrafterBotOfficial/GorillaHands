#!/bin/bash

GORILLA_DIR=".local/share/Steam/steamapps/common/Gorilla Tag/"
REPO_DIR="Projects/GorillaHands"

alacritty --working-directory "$REPO_DIR" -e nvim &
sleep 1 # Fix annoying crashing from broken plugin
alacritty --working-directory "$GORILLA_DIR" &
sleep 1
alacritty --working-directory "$REPO_DIR" &
hyprctl dispatch layoutmsg setlayout master # https://github.com/zakk4223/hyprWorkspaceLayouts
