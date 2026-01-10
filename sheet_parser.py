import sys
import os
import re
from concurrent.futures import ThreadPoolExecutor, as_completed
import traceback


MAX_THREADS = 10


# Regex to find <entry ... name="...">...</entry>
ENTRY_RE = re.compile(
    r'<entry\b[^>]*\bname\s*=\s*([\'"])(.*?)\1[^>]*>(.*?)</entry>',
    re.IGNORECASE | re.DOTALL
)

def extract_entries_from_text(content: str):
    """Return list of (entry_name, entry_text_raw) found in the file content."""
    matches = ENTRY_RE.finditer(content)
    results = []
    for m in matches:
        name = m.group(2) or ''
        inner = m.group(3) or ''
        results.append((name, inner))
    return results

def process_xml(path: str) -> str:
    """Read the file as text, extract entries via regex, write .txt output."""
    try:
        if not os.path.isfile(path):
            return f"SKIP (not a file): {path}"

        _, ext = os.path.splitext(path)
        if ext.lower() != '.xml':
            return f"SKIP (not xml): {path}"

        base = os.path.basename(path)
        name_no_ext = os.path.splitext(base)[0]

        # split filename into two halves
        if '_' in name_no_ext:
            first_half, second_half = name_no_ext.split('_', 1)
        else:
            first_half, second_half = name_no_ext, ''

        try:
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
        except UnicodeDecodeError:
            # fallback: try latin-1
            with open(path, 'r', encoding='latin-1') as f:
                content = f.read()

        entries = extract_entries_from_text(content)

        out_path = os.path.join(os.path.dirname(path), f"{name_no_ext}.txt")
        with open(out_path, 'w', encoding='utf-8', newline='\n') as out_f:
            for entry_name, entry_raw in entries:
                line = f"{first_half}>{second_half}>{entry_name}>{entry_raw}"
                out_f.write(line + "\n")

        return f"OK: {path} -> {out_path} ({len(entries)} entries)"
    except Exception:
        tb = traceback.format_exc()
        return f"ERROR processing {path}: {tb}"


def main(argv):
    if len(argv) <= 1:
        print("No files provided. Drag & drop xml files onto this script or run:")
        input("Press Enter to exit...")
        return 1

    # normalize input paths (remove surrounding quotes Windows may add)
    paths = [os.path.abspath(p.strip('"')) for p in argv[1:] if p.strip()]

    max_workers = min(MAX_THREADS, max(1, len(paths)))
    print(f"Processing {len(paths)} file(s)...")

    results = []
    with ThreadPoolExecutor(max_workers=max_workers) as ex:
        futures = {ex.submit(process_xml, p): p for p in paths}
        for fut in as_completed(futures):
            p = futures[fut]
            try:
                res = fut.result()
            except Exception as e:
                res = f"ERROR (unhandled) for {p}: {e}"
            print(res)
            results.append(res)

    ok = sum(1 for r in results if r.startswith("OK:"))
    skip = sum(1 for r in results if r.startswith("SKIP"))
    err = len(results) - ok - skip

    print("\nSummary:")
    print(f"  OK:   {ok}")
    print(f"  SKIP: {skip}")
    print(f"  ERR:  {err}")

    input("\nFinished. Press Enter to exit...")
    return 0 if err == 0 else 2


if __name__ == '__main__':
    sys.exit(main(sys.argv))