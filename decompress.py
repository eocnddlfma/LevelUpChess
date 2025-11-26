import brotli
import os

# 실제 빌드 파일들이 있는 경로
build_dir = os.path.join("Build", "Build")

files_to_decompress = [
    "Build.data.br",
    "Build.framework.js.br",
    "Build.wasm.br"
]

print(f"Looking for files in: {os.path.abspath(build_dir)}")

for filename in files_to_decompress:
    input_path = os.path.join(build_dir, filename)
    output_path = os.path.join(build_dir, filename[:-3])  # .br 제거
    
    print(f"Input: {os.path.abspath(input_path)}")
    print(f"Output: {os.path.abspath(output_path)}")
    
    if os.path.exists(input_path):
        print(f"✓ Decompressing {filename}...")
        try:
            with open(input_path, 'rb') as f:
                compressed_data = f.read()
            
            decompressed_data = brotli.decompress(compressed_data)
            
            with open(output_path, 'wb') as f:
                f.write(decompressed_data)
            
            print(f"  ✓ Complete: {filename} → {os.path.basename(output_path)}")
        except Exception as e:
            print(f"  ✗ Error: {e}")
    else:
        print(f"✗ {filename} not found at {os.path.abspath(input_path)}")

print("\nDecompression finished!")
