#!/bin/bash
# Records what a cna-cs game actually sends to the audio device, and the device's own view of the
# stream, so a "no sound" report can be resolved from evidence instead of from listening.
#
# Run it INSTEAD of launching the game yourself, and reproduce the silence while it runs:
#
#     ./scripts/Capture-GameAudio.sh <path-to-game-dll> [seconds]
#
# It answers three separate questions that a listener cannot tell apart:
#   1. did the game produce audio at all          -> the mixer capture
#   2. did that audio reach the device            -> the sink-monitor capture
#   3. did the device accept it                   -> the corked/muted/volume dump
set -u
GAME=${1:?usage: audio-capture.sh <game.dll> [seconds]}
SECONDS_TO_RUN=${2:-40}
export XDG_RUNTIME_DIR=${XDG_RUNTIME_DIR:-/run/user/$(id -u)}

# The game resolves its native library from the environment, so a capture run has to launch it the
# same way the reporter does. Without this the game dies in its constructor, and every audio
# question below then answers "no" for a reason that has nothing to do with audio.
DEFAULT_NATIVE=/rv/data/development/github.com/openeggbert/cnanext/cmake-build-debug/modules/c-api/libcna_c_api.so
if [ -z "${CNA_NATIVE_LIBRARY:-}" ] && [ -z "${CNA_NATIVE_DIR:-}" ]; then
    if [ -f "$DEFAULT_NATIVE" ]; then
        export CNA_NATIVE_LIBRARY=$DEFAULT_NATIVE
    else
        echo "No CNA_NATIVE_LIBRARY set and $DEFAULT_NATIVE does not exist." >&2
        echo "Set CNA_NATIVE_LIBRARY to the library you actually run the game with." >&2
        exit 2
    fi
fi
echo "native lib   : ${CNA_NATIVE_LIBRARY:-(directory: $CNA_NATIVE_DIR)}"
HERE=$(cd "$(dirname "$0")/.." && pwd)
OUT=$HERE/build-probe/audio-capture
mkdir -p "$OUT"

SINK=$(pactl get-default-sink)
echo "default sink : $SINK"
echo "sink state   : $(pactl list short sinks | grep -F "$SINK" | awk '{print $NF}')"
echo "sink mute    : $(pactl get-sink-mute "$SINK")"
echo "sink volume  : $(pactl get-sink-volume "$SINK" | head -1)"

parec -d "${SINK}.monitor" --format=s16le --rate=44100 --channels=2 --file-format=raw \
    > "$OUT/speaker.raw" 2>/dev/null &
REC=$!

( cd "$(dirname "$GAME")" && timeout "$SECONDS_TO_RUN" dotnet "$(basename "$GAME")" ) \
    > "$OUT/game.log" 2>&1 &
PID=$!

sleep 8

# Distinguish "the game is running and silent" from "the game is not running". They produce the
# same captures and mean entirely different things.
if ! kill -0 $PID 2>/dev/null; then
    wait $PID 2>/dev/null
    kill $REC 2>/dev/null
    echo
    echo "The game exited before the capture window opened. This is not an audio result."
    echo "Its last words:"
    tail -20 "$OUT/game.log" | sed 's/^/    /'
    exit 1
fi

echo
echo "=== the game's own stream, as the audio server sees it ==="
pactl list sink-inputs | awk '/Sink Input #/{keep=$0; buf=""} {buf=buf"\n"$0} /application\.name/{if (buf ~ /SDL|dotnet/) print keep buf}' \
  | grep -E "Sink Input #|Sink:|Corked:|Mute:|Volume: front-left|application.name" \
  || echo "  (the game is running but has opened no audio device -- that is itself the finding)"

wait $PID 2>/dev/null
sleep 1; kill $REC 2>/dev/null

echo
python3 - "$OUT/speaker.raw" <<'PY'
import struct, math, sys
data = open(sys.argv[1], "rb").read()
s = struct.unpack(f"<{len(data)//2}h", data[:len(data)//2*2])
left = s[0::2]
if not left:
    print("nothing captured from the sink monitor"); raise SystemExit
print(f"reached the speakers: {len(left)/44100:.1f}s captured, peak={max(abs(v) for v in left)}")
bars = []
for i in range(0, len(left), 22050):
    w = left[i:i+22050]
    rms = math.sqrt(sum(v*v for v in w)/len(w)) if w else 0
    bars.append("#" if rms > 1000 else ("+" if rms > 100 else ("." if rms > 0 else " ")))
print("500ms cells: |" + "".join(bars) + "|")
PY
echo
echo "renderer: $(grep -m1 -i 'graphics renderer' "$OUT/game.log" || echo unknown)"
echo "logs and raw capture in $OUT"
