import os
import shutil

try:
    import brotli
    HAS_BROTLI = True
except ImportError:
    HAS_BROTLI = False
    print("⚠️ brotli 모듈이 설치되지 않음. 압축 해제를 건너뜁니다.")
    print("   설치하려면: pip install brotli")

# 빌드 디렉토리 설정
build_root = "build"
build_subfolder = os.path.join(build_root, "Build")  # Unity가 생성하는 Build 하위폴더

print("=" * 50)
print("Unity WebGL 빌드 후처리 스크립트")
print("=" * 50)

# 1. Build 하위 폴더가 있으면 처리
if os.path.exists(build_subfolder):
    print("\n[1단계] Build 하위 폴더 발견 - 파일 이동 중...")
    
    # Brotli 압축 해제 (Build 폴더 내)
    if HAS_BROTLI:
        files_to_decompress = [
            "build.data.br",
            "build.framework.js.br", 
            "build.wasm.br"
        ]
        
        for filename in files_to_decompress:
            input_path = os.path.join(build_subfolder, filename)
            output_path = os.path.join(build_subfolder, filename[:-3])  # .br 제거
            
            if os.path.exists(input_path):
                print(f"  압축 해제: {filename}")
                try:
                    with open(input_path, 'rb') as f:
                        compressed_data = f.read()
                    
                    decompressed_data = brotli.decompress(compressed_data)
                    
                    with open(output_path, 'wb') as f:
                        f.write(decompressed_data)
                    
                    os.remove(input_path)
                    print(f"    ✓ 완료")
                except Exception as e:
                    print(f"    ✗ 오류: {e}")
    
    # Build 폴더 내 파일들을 build 루트로 이동
    for filename in os.listdir(build_subfolder):
        src = os.path.join(build_subfolder, filename)
        dst = os.path.join(build_root, filename)
        
        if os.path.isfile(src):
            if os.path.exists(dst):
                os.remove(dst)
            shutil.move(src, dst)
            print(f"  이동: {filename}")
        elif os.path.isdir(src):
            if os.path.exists(dst):
                shutil.rmtree(dst)
            shutil.move(src, dst)
            print(f"  이동: {filename}/")
    
    # 빈 Build 폴더 삭제
    if os.path.exists(build_subfolder) and len(os.listdir(build_subfolder)) == 0:
        os.rmdir(build_subfolder)
        print("  삭제: 빈 Build 폴더")
else:
    print("\n[1단계] Build 하위 폴더 없음 - 건너뜀")
    
    # build 루트에 .br 파일이 있으면 압축 해제
    if HAS_BROTLI:
        files_to_decompress = [
            "build.data.br",
            "build.framework.js.br", 
            "build.wasm.br"
        ]
        
        for filename in files_to_decompress:
            input_path = os.path.join(build_root, filename)
            output_path = os.path.join(build_root, filename[:-3])
            
            if os.path.exists(input_path):
                print(f"  압축 해제: {filename}")
                try:
                    with open(input_path, 'rb') as f:
                        compressed_data = f.read()
                    
                    decompressed_data = brotli.decompress(compressed_data)
                    
                    with open(output_path, 'wb') as f:
                        f.write(decompressed_data)
                    
                    os.remove(input_path)
                    print(f"    ✓ 완료")
                except Exception as e:
                    print(f"    ✗ 오류: {e}")

# 2. build/index.html 수정 (build 폴더 내에서 실행할 때용)
print("\n[2단계] build/index.html 수정...")

build_index_path = os.path.join(build_root, "index.html")
if os.path.exists(build_index_path):
    with open(build_index_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original = content
    
    # buildUrl을 현재 디렉토리로 설정
    content = content.replace('var buildUrl = "Build";', 'var buildUrl = ".";')
    content = content.replace('var buildUrl = "Build/Build";', 'var buildUrl = ".";')
    
    # .br 확장자 제거
    content = content.replace('.data.br', '.data')
    content = content.replace('.framework.js.br', '.framework.js')
    content = content.replace('.wasm.br', '.wasm')
    
    # 대문자 Build.xxx → 소문자 build.xxx
    content = content.replace('/Build.loader.js', '/build.loader.js')
    content = content.replace('/Build.data', '/build.data')
    content = content.replace('/Build.framework.js', '/build.framework.js')
    content = content.replace('/Build.wasm', '/build.wasm')
    content = content.replace('"Build.loader.js', '"build.loader.js')
    
    if content != original:
        with open(build_index_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print("  ✓ build/index.html 수정 완료")
    else:
        print("  - 변경 사항 없음")
else:
    print("  ✗ build/index.html 없음")

# 3. 루트 index.html 수정 (GitHub Pages / 루트에서 실행할 때용)
print("\n[3단계] 루트 index.html 수정...")

root_index_path = "index.html"
if os.path.exists(root_index_path):
    with open(root_index_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original = content
    
    # buildUrl을 build 폴더로 설정
    content = content.replace('var buildUrl = ".";', 'var buildUrl = "build";')
    content = content.replace('var buildUrl = "Build";', 'var buildUrl = "build";')
    content = content.replace('var buildUrl = "Build/Build";', 'var buildUrl = "build";')
    
    # .br 확장자 제거
    content = content.replace('.data.br', '.data')
    content = content.replace('.framework.js.br', '.framework.js')
    content = content.replace('.wasm.br', '.wasm')
    
    # 대문자 Build.xxx → 소문자 build.xxx  
    content = content.replace('/Build.loader.js', '/build.loader.js')
    content = content.replace('/Build.data', '/build.data')
    content = content.replace('/Build.framework.js', '/build.framework.js')
    content = content.replace('/Build.wasm', '/build.wasm')
    content = content.replace('"Build.loader.js', '"build.loader.js')
    
    # CSS/favicon 경로를 build 폴더로
    content = content.replace('href="TemplateData/', 'href="build/')
    content = content.replace('href="Build/TemplateData/', 'href="build/')
    
    # StreamingAssets 경로 수정
    content = content.replace('streamingAssetsUrl: "StreamingAssets"', 'streamingAssetsUrl: "build/StreamingAssets"')
    
    if content != original:
        with open(root_index_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print("  ✓ 루트 index.html 수정 완료")
    else:
        print("  - 변경 사항 없음")
else:
    print("  ✗ 루트 index.html 없음")

# 4. 최종 확인
print("\n" + "=" * 50)
print("완료!")
print("=" * 50)

print("\n📁 build 폴더 구조:")
if os.path.exists(build_root):
    for item in sorted(os.listdir(build_root)):
        item_path = os.path.join(build_root, item)
        if os.path.isdir(item_path):
            print(f"  📁 {item}/")
        else:
            size = os.path.getsize(item_path)
            if size > 1024 * 1024:
                print(f"  📄 {item} ({size / 1024 / 1024:.1f} MB)")
            elif size > 1024:
                print(f"  📄 {item} ({size / 1024:.1f} KB)")
            else:
                print(f"  📄 {item} ({size} B)")

print("\n💡 테스트 방법:")
print("  1. 터미널에서: python -m http.server 8000")
print("  2. 브라우저에서: http://localhost:8000")
