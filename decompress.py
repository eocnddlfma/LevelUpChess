import brotli
import os

build_dir = "Build"

files_to_decompress = [
    "BasicChessWeb.data.br",
    "BasicChessWeb.framework.js.br",
    "BasicChessWeb.wasm.br"
]

for filename in files_to_decompress:
    input_path = os.path.join(build_dir, filename)
    output_path = os.path.join(build_dir, filename[:-3])  # .br 제거
    
    if os.path.exists(input_path):
        print(f"Decompressing {filename}...")
        with open(input_path, 'rb') as f:
            compressed_data = f.read()
        
        decompressed_data = brotli.decompress(compressed_data)
        
        with open(output_path, 'wb') as f:
            f.write(decompressed_data)
        
        print(f"✓ {filename} → {output_path}")
    else:
        print(f"✗ {filename} not found")

print("\nDecompression complete!")
