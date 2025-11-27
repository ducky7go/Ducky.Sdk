#!/bin/bash
# AOS Notification Helper
# Usage: ./notify.sh <type> <title> <message>
# Types: start, progress, milestone, complete, warning, error

TYPE="${1:-info}"
TITLE="${2:-AOS Notification}"
MESSAGE="${3:-}"

# Notification type mapping
case "$TYPE" in
    start|progress|info)
        SOUND="/usr/share/sounds/freedesktop/stereo/dialog-information.oga"
        ;;
    milestone|complete|success)
        SOUND="/usr/share/sounds/freedesktop/stereo/complete.oga"
        ;;
    warning)
        SOUND="/usr/share/sounds/freedesktop/stereo/dialog-warning.oga"
        ;;
    error)
        SOUND="/usr/share/sounds/freedesktop/stereo/dialog-error.oga"
        ;;
    *)
        SOUND="/usr/share/sounds/freedesktop/stereo/dialog-information.oga"
        ;;
esac

# Send desktop notification
notify-send "$TITLE" "$MESSAGE" 2>/dev/null &

# Play sound
paplay "$SOUND" 2>/dev/null &
