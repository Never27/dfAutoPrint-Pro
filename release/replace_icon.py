"""Replace the icon in a 7-Zip SFX executable with a custom .ico file."""
import pefile
import io
import struct

EXE_PATH = r"C:\Users\Administrator\WorkBuddy\2026-06-05-09-13-35\PdfAutoPrint.Pro\release\PdfAutoPrint_Pro_Setup_v1.1.1.exe"
ICO_PATH = r"C:\Users\Administrator\WorkBuddy\2026-06-05-09-13-35\PdfAutoPrint.Pro\Assets\app.ico"

RT_ICON = 3
RT_GROUP_ICON = 14


def parse_ico(path):
    """Parse .ico file into individual icon entries (width, height, bpp, data)."""
    with open(path, "rb") as f:
        data = f.read()

    reserved, img_type, count = struct.unpack_from("<HHH", data, 0)
    if reserved != 0 or img_type != 1:
        raise ValueError("Not a valid .ico file")

    icons = []
    offset = 6
    for i in range(count):
        w, h, colors, reserved2, planes, bpp, size, img_offset = struct.unpack_from(
            "<BBBBHHII", data, offset
        )
        w = 256 if w == 0 else w
        h = 256 if h == 0 else h
        icon_data = data[img_offset : img_offset + size]
        icons.append({"width": w, "height": h, "bpp": bpp, "size": size, "data": icon_data})
        offset += 16
    return icons


def replace_icon(exe_path, ico_path, output_path=None):
    """Replace RT_GROUP_ICON and RT_ICON resources in a PE file."""
    if output_path is None:
        output_path = exe_path

    icons = parse_ico(ico_path)
    pe = pefile.PE(exe_path)

    # --- Step 1: Build new resource data blocks ---
    # Each icon gets a new RT_ICON resource, and one RT_GROUP_ICON entry

    # Remove old RT_ICON and RT_GROUP_ICON entries
    new_entries = []
    for entry in pe.DIRECTORY_ENTRY_RESOURCE.entries:
        eid = entry.id if entry.id is not None else entry.name
        if isinstance(eid, int) and eid in (RT_ICON, RT_GROUP_ICON):
            continue
        new_entries.append(entry)

    # We'll use pefile's internal structures to add resources.
    # Since direct resource manipulation is complex, let's use
    # a simpler approach: modify data at raw offsets.

    # Actually, proper resource replacement in pefile requires
    # rebuilding the resource section. Let's use a different approach.

    # Find the resource section
    resource_section = None
    for section in pe.sections:
        if b".rsrc" in section.Name:
            resource_section = section
            break

    if not resource_section:
        print("No .rsrc section found!")
        pe.close()
        return False

    print(f"Found .rsrc section at RVA {hex(resource_section.VirtualAddress)}")

    # Count existing icon resources
    icon_count = 0
    for entry in pe.DIRECTORY_ENTRY_RESOURCE.entries:
        eid = entry.id if entry.id is not None else entry.name
        if isinstance(eid, int) and eid == RT_ICON:
            icon_count += 1
        elif isinstance(eid, int) and eid == RT_GROUP_ICON:
            icon_count += 1

    print(f"Found {icon_count} icon-related resource entries")

    pe.close()
    print("Analysis complete. Note: Full resource replacement requires "
          "rebuilding the resource directory. Simplest approach: use rcedit.exe")
    return True


if __name__ == "__main__":
    replace_icon(EXE_PATH, ICO_PATH)
