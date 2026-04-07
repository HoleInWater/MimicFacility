#!/usr/bin/env bash
#
# version_bump.sh — MimicFacility depth-based versioning
#
# Version format: {GreekPhase}-{d0}.{d1}.{d2}.{d3}.{d4}...
#   - Phase: Greek alphabet (Alpha, Beta, Gamma, ...)
#   - Each segment maps to a folder depth from repo root
#   - Depth 0 = repo root files, depth 1 = one folder deep, etc.
#   - Shallowest change increments its segment and resets all deeper segments
#   - When manually advancing the Greek phase, all segments reset to 0
#
# Usage:
#   ./Tools/version_bump.sh              # auto-detect from last commit
#   ./Tools/version_bump.sh --diff HEAD~3 # compare against specific ref
#   ./Tools/version_bump.sh --advance-phase  # move to next Greek phase
#   ./Tools/version_bump.sh --dry-run     # show what would change, don't write

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
VERSION_FILE="$REPO_ROOT/VERSION"

GREEK_PHASES=(
    "Alpha" "Beta" "Gamma" "Delta" "Epsilon"
    "Zeta" "Eta" "Theta" "Iota" "Kappa"
    "Lambda" "Mu" "Nu" "Xi" "Omicron"
    "Pi" "Rho" "Sigma" "Tau" "Upsilon"
    "Phi" "Chi" "Psi" "Omega"
)

DRY_RUN=false
ADVANCE_PHASE=false
DIFF_REF="HEAD~1"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --dry-run)    DRY_RUN=true; shift ;;
        --advance-phase) ADVANCE_PHASE=true; shift ;;
        --diff)       DIFF_REF="$2"; shift 2 ;;
        -h|--help)
            echo "Usage: version_bump.sh [--dry-run] [--advance-phase] [--diff REF]"
            exit 0 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

if [[ ! -f "$VERSION_FILE" ]]; then
    echo "Alpha-0" > "$VERSION_FILE"
fi

CURRENT_VERSION="$(cat "$VERSION_FILE" | tr -d '[:space:]')"

CURRENT_PHASE="${CURRENT_VERSION%%-*}"
CURRENT_SEGMENTS="${CURRENT_VERSION#*-}"

IFS='.' read -ra SEGMENTS <<< "$CURRENT_SEGMENTS"

get_phase_index() {
    local phase="$1"
    for i in "${!GREEK_PHASES[@]}"; do
        if [[ "${GREEK_PHASES[$i]}" == "$phase" ]]; then
            echo "$i"
            return
        fi
    done
    echo "0"
}

if $ADVANCE_PHASE; then
    phase_idx=$(get_phase_index "$CURRENT_PHASE")
    next_idx=$((phase_idx + 1))

    if [[ $next_idx -ge ${#GREEK_PHASES[@]} ]]; then
        echo "ERROR: Already at final phase (Omega). Cannot advance further."
        exit 1
    fi

    NEW_PHASE="${GREEK_PHASES[$next_idx]}"
    NEW_VERSION="$NEW_PHASE-0"

    echo "Phase advance: $CURRENT_PHASE -> $NEW_PHASE"
    echo "New version: $NEW_VERSION"

    if ! $DRY_RUN; then
        echo "$NEW_VERSION" > "$VERSION_FILE"
        echo "VERSION file updated."
    else
        echo "(dry run — no changes written)"
    fi
    exit 0
fi

CHANGED_FILES=$(git diff --name-only "$DIFF_REF" 2>/dev/null || true)

if [[ -z "$CHANGED_FILES" ]]; then
    echo "No changed files detected (compared to $DIFF_REF). Version unchanged."
    echo "Current: $CURRENT_VERSION"
    exit 0
fi

SHALLOWEST_DEPTH=999

echo "Changed files:"
while IFS= read -r file; do
    dir="$(dirname "$file")"

    if [[ "$dir" == "." ]]; then
        depth=0
    else
        depth=$(echo "$dir" | tr '/' '\n' | wc -l)
    fi

    echo "  depth $depth: $file"

    if [[ $depth -lt $SHALLOWEST_DEPTH ]]; then
        SHALLOWEST_DEPTH=$depth
    fi
done <<< "$CHANGED_FILES"

MAX_DEPTH=0
while IFS= read -r file; do
    dir="$(dirname "$file")"
    if [[ "$dir" == "." ]]; then
        depth=0
    else
        depth=$(echo "$dir" | tr '/' '\n' | wc -l)
    fi
    if [[ $depth -gt $MAX_DEPTH ]]; then
        MAX_DEPTH=$depth
    fi
done <<< "$CHANGED_FILES"

NEEDED_SEGMENTS=$((MAX_DEPTH + 1))

while [[ ${#SEGMENTS[@]} -lt $NEEDED_SEGMENTS ]]; do
    SEGMENTS+=(0)
done

SEGMENTS[$SHALLOWEST_DEPTH]=$(( ${SEGMENTS[$SHALLOWEST_DEPTH]} + 1 ))

for (( i = SHALLOWEST_DEPTH + 1; i < ${#SEGMENTS[@]}; i++ )); do
    SEGMENTS[$i]=0
done

NEW_SEGMENTS=""
for (( i = 0; i < ${#SEGMENTS[@]}; i++ )); do
    if [[ $i -gt 0 ]]; then
        NEW_SEGMENTS+="."
    fi
    NEW_SEGMENTS+="${SEGMENTS[$i]}"
done

# Trim trailing .0 segments that are all zero beyond the shallowest change
# but keep at least up to the shallowest changed depth + 1
MIN_KEEP=$((SHALLOWEST_DEPTH + 1))
IFS='.' read -ra FINAL_SEGS <<< "$NEW_SEGMENTS"

LAST_NONZERO=0
for (( i = 0; i < ${#FINAL_SEGS[@]}; i++ )); do
    if [[ ${FINAL_SEGS[$i]} -ne 0 ]]; then
        LAST_NONZERO=$i
    fi
done

KEEP_UP_TO=$LAST_NONZERO
if [[ $MIN_KEEP -gt $((KEEP_UP_TO + 1)) ]]; then
    KEEP_UP_TO=$((MIN_KEEP - 1))
fi

TRIMMED=""
for (( i = 0; i <= KEEP_UP_TO; i++ )); do
    if [[ $i -gt 0 ]]; then
        TRIMMED+="."
    fi
    TRIMMED+="${FINAL_SEGS[$i]}"
done

NEW_VERSION="$CURRENT_PHASE-$TRIMMED"

echo ""
echo "Shallowest change at depth: $SHALLOWEST_DEPTH"
echo "Deepest file at depth:      $MAX_DEPTH"
echo "Version: $CURRENT_VERSION -> $NEW_VERSION"

if ! $DRY_RUN; then
    echo "$NEW_VERSION" > "$VERSION_FILE"
    echo "VERSION file updated."
else
    echo "(dry run — no changes written)"
fi
