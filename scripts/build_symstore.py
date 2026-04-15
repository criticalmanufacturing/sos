import os
import struct
import shutil
import glob
import argparse

# This script processes Portable PDB files to generate a symbol server structure compliant with the SSQP format.
# SSQP (Symbol Server Quick Path) is a format used by Microsoft Visual Studio's debugger to efficiently locate and load symbols (vsdbg) for debugging purposes.
# Usage: python build_symstore.py -i <input_directory_with_pdbs> -o <output_directory_for_symbol_server>
def get_ssqp_signature(pdb_path):
    """Parses a Portable PDB to extract the SSQP Signature string."""
    try:
        with open(pdb_path, 'rb') as f:
            # 1. Verify Portable PDB Magic Signature (BSJB)
            magic = f.read(4)
            if magic != b'BSJB':
                return None # Not a Portable PDB (might be a classic Windows PDB)

            # 2. Skip to the version string length
            f.seek(12) 
            version_len = struct.unpack('<I', f.read(4))[0]

            # 3. Skip the padded version string and flags to get to Streams count
            padded_version_len = (version_len + 3) & ~3
            f.seek(16 + padded_version_len + 2) # +2 for Flags
            
            streams_count = struct.unpack('<H', f.read(2))[0]

            # 4. Iterate over Stream Headers to find the '#Pdb' stream
            for _ in range(streams_count):
                offset, size = struct.unpack('<II', f.read(8))

                # Read null-terminated, 4-byte padded stream name
                name_bytes = bytearray()
                while True:
                    b = f.read(1)
                    if not b or b == b'\x00':
                        break
                    name_bytes.append(b[0])
                
                name = name_bytes.decode('ascii')

                # Skip the 4-byte alignment padding for the name
                name_len_with_null = len(name) + 1
                padded_name_len = (name_len_with_null + 3) & ~3
                f.read(padded_name_len - name_len_with_null)

                # 5. Extract the 20-byte PDB ID
                if name == '#Pdb':
                    current_pos = f.tell() # Save position to continue loop if needed
                    f.seek(offset)
                    pdb_id = f.read(20)
                    
                    # 6. Apply Microsoft GUID Little-Endian byte-swapping to the first 8 bytes
                    p1, p2, p3 = struct.unpack('<IHH', pdb_id[0:8])
                    # The last 8 bytes are standard big-endian sequence (force lowercase)
                    p4 = pdb_id[8:16].hex().lower()

                    # 7. Format into the 32-character lowercase string
                    signature = f"{p1:08x}{p2:04x}{p3:04x}{p4}"
                    
                    # 8. Append the Portable PDB Age mandate (keep FFFFFFFF uppercase)
                    return f"{signature}FFFFFFFF"
                    
                    f.seek(current_pos)
                    
    except Exception as e:
        print(f"Error parsing {pdb_path}: {e}")
        return None
        
    return None

def build_symbol_server(input_dir, output_dir):
    """Processes flat PDBs into an SSQP-compliant directory structure."""
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)

    search_pattern = os.path.join(input_dir, '*.pdb')
    pdb_files = glob.glob(search_pattern)

    if not pdb_files:
        print(f"No .pdb files found in {input_dir}")
        return

    for pdb_path in pdb_files:
        filename = os.path.basename(pdb_path)
        lowercase_filename = filename.lower()
        
        print(f"Processing: {filename}")
        
        signature_folder = get_ssqp_signature(pdb_path)
        
        if signature_folder:
            # Build the exact path vsdbg expects: lowercase_name/SIGNATURE/lowercase_name
            target_dir = os.path.join(output_dir, lowercase_filename, signature_folder)
            os.makedirs(target_dir, exist_ok=True)
            
            target_file = os.path.join(target_dir, lowercase_filename)
            shutil.copy2(pdb_path, target_file)
            
            print(f"  -> Created: {lowercase_filename}/{signature_folder}/{lowercase_filename}")
        else:
            print(f"  -> Skipped: Could not extract Portable PDB signature.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate SSQP Symbol Server from Portable PDBs")
    parser.add_argument("-i", "--input", required=True, help="Directory containing flat .pdb files")
    parser.add_argument("-o", "--output", required=True, help="Target directory for the Nginx symbol server")
    args = parser.parse_args()

    build_symbol_server(args.input, args.output)
    print("\nSymbol server generation complete!")