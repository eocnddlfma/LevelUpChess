import brotli
import os
import shutil

# 빌드 디렉토리 설정
build_root = "build"
build_folder = os.path.join(build_root, "Build")

# 1. Brotli 압축 해제
print("=" * 50)
print("1. Brotli 압축 해제")
print("=" * 50)

files_to_decompress = [
    "build.data.br",
    "build.framework.js.br", 
    "build.wasm.br"
]

for filename in files_to_decompress:
    input_path = os.path.join(build_folder, filename)
    output_path = os.path.join(build_folder, filename[:-3])  # .br 제거
    
    if os.path.exists(input_path):
        print(f"압축 해제 중: {filename}")
        try:
            with open(input_path, 'rb') as f:
                compressed_data = f.read()
            
            decompressed_data = brotli.decompress(compressed_data)
            
            with open(output_path, 'wb') as f:
                f.write(decompressed_data)
            
            print(f"  ✓ 완료: {filename} → {os.path.basename(output_path)}")
            
            # 압축 파일 삭제 (선택사항)
            os.remove(input_path)
            print(f"  ✓ 삭제: {filename}")
        except Exception as e:
            print(f"  ✗ 오류: {e}")
    else:
        print(f"✗ 파일 없음: {input_path}")

# 2. 파일들을 루트로 이동하고 index.html 수정
print("\n" + "=" * 50)
print("2. 파일 구조 변경 (루트로 이동)")
print("=" * 50)

# Build 폴더 내 파일들을 루트로 이동
build_files = os.listdir(build_folder)
for filename in build_files:
    src = os.path.join(build_folder, filename)
    dst = os.path.join(build_root, filename)
    if os.path.isfile(src):
        shutil.move(src, dst)
        print(f"이동: {filename} → 루트")

# 빈 Build 폴더 삭제
if os.path.exists(build_folder) and len(os.listdir(build_folder)) == 0:
    os.rmdir(build_folder)
    print("삭제: 빈 Build 폴더")

# TemplateData 폴더 내 파일들도 루트로 이동
template_folder = os.path.join(build_root, "TemplateData")
if os.path.exists(template_folder):
    template_files = os.listdir(template_folder)
    for filename in template_files:
        src = os.path.join(template_folder, filename)
        dst = os.path.join(build_root, filename)
        if os.path.isfile(src):
            shutil.move(src, dst)
            print(f"이동: TemplateData/{filename} → 루트")
    
    # 빈 TemplateData 폴더 삭제
    if len(os.listdir(template_folder)) == 0:
        os.rmdir(template_folder)
        print("삭제: 빈 TemplateData 폴더")

# 3. index.html 경로 수정
print("\n" + "=" * 50)
print("3. index.html 경로 수정")
print("=" * 50)

index_path = os.path.join(build_root, "index.html")
if os.path.exists(index_path):
    with open(index_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 경로 수정 - Build/ 와 TemplateData/ 접두사 제거
    original_content = content
    
    # Build 폴더 경로 수정
    content = content.replace('var buildUrl = "Build";', 'var buildUrl = ".";')
    content = content.replace('buildUrl + "/build.', '"build.')
    
    # .br 확장자를 제거된 파일로 변경
    content = content.replace('build.data.br', 'build.data')
    content = content.replace('build.framework.js.br', 'build.framework.js')
    content = content.replace('build.wasm.br', 'build.wasm')
    
    # TemplateData 경로 수정
    content = content.replace('TemplateData/', '')
    
    with open(index_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print("✓ index.html 경로 수정 완료")
    print("  - Build/ → . (루트)")
    print("  - TemplateData/ → 루트")
    print("  - .br 확장자 제거")
else:
    print("✗ index.html을 찾을 수 없습니다")

print("\n" + "=" * 50)
print("완료!")
print("=" * 50)

# 최종 파일 목록 출력
print("\n최종 파일 구조:")
for item in sorted(os.listdir(build_root)):
    item_path = os.path.join(build_root, item)
    if os.path.isdir(item_path):
        print(f"  📁 {item}/")
    else:
        print(f"  📄 {item}")
