#!/usr/bin/env python3
import sys
import xml.etree.ElementTree as ET
import gzip

def get_local_tag(tag):
    # Remove any namespace from the tag
    return tag.split('}')[-1] if '}' in tag else tag

def extract_descriptions(filename):
    # Read the file as text (handling gzipped files if necessary)
    try:
        if filename.endswith('.gz'):
            with gzip.open(filename, 'rb') as f:
                data = f.read().decode('utf-8', errors='ignore')
        else:
            with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
                data = f.read()
    except Exception as e:
        print(f"Error reading {filename}: {e}", file=sys.stderr)
        return set()

    # If the file sample is truncated, its last character may not be ">"
    if not data.rstrip().endswith('>'):
        # Find the last complete tag (last occurrence of ">")
        last_gt = data.rfind('>')
        if last_gt != -1:
            data = data[:last_gt+1]
        else:
            print(f"No complete XML tag found in {filename}", file=sys.stderr)
            return set()

    try:
        root = ET.fromstring(data)
    except Exception as e:
        print(f"Error parsing XML from {filename}: {e}", file=sys.stderr)
        return set()

    descriptions = set()

    # Iterate through all elements looking for clinical significance descriptions
    for elem in root.iter():
        local_tag = get_local_tag(elem.tag)
        if local_tag == "Interpretation" and elem.attrib.get("Type") == "Clinical significance":
            for child in elem:
                if get_local_tag(child.tag) == "Description" and child.text:
                    descriptions.add(child.text.strip())
        elif local_tag == "ClinicalSignificance":
            for child in elem:
                if get_local_tag(child.tag) == "Description" and child.text:
                    descriptions.add(child.text.strip())
    return descriptions

def main():
    if len(sys.argv) < 2:
        print(f"Usage: {sys.argv[0]} <xml_file1> [<xml_file2> ...]", file=sys.stderr)
        sys.exit(1)

    all_descriptions = set()
    for filename in sys.argv[1:]:
        all_descriptions |= extract_descriptions(filename)

    # Print sorted unique descriptions
    for desc in sorted(all_descriptions):
        print(desc)

if __name__ == '__main__':
    main()

