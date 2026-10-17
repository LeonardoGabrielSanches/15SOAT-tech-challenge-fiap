#!/usr/bin/env bash
set -euo pipefail

limite="${LIMITE_COBERTURA:-80}"

rm -rf TestResults coverage
dotnet test Oficina.sln --configuration Release --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults

dotnet tool restore
dotnet tool run reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"Html;TextSummary;JsonSummary"

linhas=$(sed -n 's/^ *Line coverage: *\([0-9.]*\)%.*/\1/p' coverage/Summary.txt | head -1)
echo "Cobertura de linhas em Domain + Application: ${linhas}% (minimo ${limite}%)"

if ! awk -v c="$linhas" -v l="$limite" 'BEGIN { exit !(c + 0 >= l + 0) }'; then
  echo "Cobertura abaixo do minimo exigido." >&2
  exit 1
fi
