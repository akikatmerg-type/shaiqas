#!/bin/bash
# Призрачный Юнити: Core (C#) → Java → javac, сосын тексерулер.
#   ./build.sh           — компиляция + барлық тест
#   ./build.sh compile   — тек компиляция
set -e
G="$(cd "$(dirname "$0")" && pwd)"; OUT="$G/build"; cd "$G"
rm -rf "$OUT"; mkdir -p "$OUT/src" "$OUT/cls"
python3 "$G/cs2java.py" "$G/../SHAIQAS/Assets/Scripts/Core" "$OUT/src"
cp "$G"/java/*.java "$OUT/src/"
javac -nowarn -encoding UTF-8 -d "$OUT/cls" "$OUT"/src/*.java 2>&1 | grep -v -e JAVA_TOOL -e '^Note:' || true
[ -f "$OUT/cls/Ghost.class" ] || { echo "КОМПИЛЯЦИЯ СӘТСІЗ"; exit 1; }
[ "$1" = "compile" ] && exit 0
J="java -Dstdout.encoding=UTF-8 -cp $OUT/cls"
echo "== lint (Unity қабаты)"; python3 "$G/lint.py"; python3 "$G/refcheck.py" || true
echo "== логика, сюжет, сақтау, 36 ұрыс"; $J Ghost 2>&1 | grep -v JAVA_TOOL
echo "== файлдар мен обликтер"; $J Assets 2>&1 | grep -v JAVA_TOOL
