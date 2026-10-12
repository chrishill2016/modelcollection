#!/usr/bin/env bash
# Prepares a published Blazor wwwroot folder for GitHub Pages.
# Usage: prepare-pages.sh <wwwroot> <base-path>   e.g. prepare-pages.sh publish/wwwroot /modelcollection/
set -euo pipefail

root="$1"
base="$2"

# 1. Serve from the repo sub-path.
sed -i "s|<base href=\"/\" />|<base href=\"${base}\" />|" "$root/index.html"
grep -q "<base href=\"${base}\" />" "$root/index.html" || { echo "Failed to set base href" >&2; exit 1; }

# 2. index.html changed, so update its hash in the service worker manifest (the service worker
#    verifies integrity when caching for offline use) and drop the stale pre-compressed copies.
#    (The web manifest deliberately has no "id": it then defaults to the start URL, /<repo>/, which is
#    unique per app. An explicit "./" would resolve to the shared <user>.github.io root.)
for file in index.html; do
  hash="sha256-$(openssl dgst -sha256 -binary "$root/$file" | openssl base64 -A)"
  python3 - "$root/service-worker-assets.js" "$hash" "$file" <<'PY'
import re, sys
path, new_hash, name = sys.argv[1], sys.argv[2], sys.argv[3]
text = open(path).read()
pattern = r'("hash":\s*")[^"]+(",\s*"url":\s*"' + re.escape(name) + '")'
text, n = re.subn(pattern, lambda m: m.group(1) + new_hash + m.group(2), text)
if n != 1:
    sys.exit(name + " entry not found in service-worker-assets.js")
open(path, "w").write(text)
PY
  rm -f "$root/$file.br" "$root/$file.gz"
done
rm -f "$root/service-worker-assets.js.br" "$root/service-worker-assets.js.gz"

# 3. Deep links: GitHub Pages serves 404.html for unknown paths, which boots the app at that route.
cp "$root/index.html" "$root/404.html"

# 4. Stop Jekyll from hiding the _framework and _content folders.
touch "$root/.nojekyll"

echo "Prepared $root for $base"
