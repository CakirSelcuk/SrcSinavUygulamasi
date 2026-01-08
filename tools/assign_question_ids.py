#!/usr/bin/env python3
"""
SRC Sınav Uygulaması - Soru ID Atama Aracı
==========================================

Bu araç, tüm soru JSON dosyalarını tarar ve eksik ID'leri ekler.

KULLANIM:
    python assign_question_ids.py

ÇIKTI:
    - Güncellenen dosya listesi
    - Eklenen ID sayısı
    - Varsa hata listesi

FORMAT:
    ID formatı: src{N}_{examType}_q{###}
    Örnek: src4_pratik_q019
"""

import json
import os
import sys
from pathlib import Path
from typing import Dict, List, Tuple

# Renk kodları (terminal çıktısı için)
class Colors:
    RED = '\033[91m'
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    BLUE = '\033[94m'
    END = '\033[0m'

def get_exam_type_from_filename(filename: str) -> str:
    """Dosya adından sınav tipini çıkart"""
    name = filename.lower()
    if "_sinav" in name:
        return "sinav"
    elif "_resimli" in name:
        return "resimli"
    else:
        return "pratik"

def get_category_from_filename(filename: str) -> str:
    """Dosya adından SRC kategorisini çıkart"""
    name = filename.lower()
    for i in range(1, 6):
        if f"src{i}" in name:
            return f"src{i}"
    return "src0"

def load_json_file(filepath: Path) -> Tuple[List[Dict], bool]:
    """JSON dosyasını yükle"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        return data, True
    except Exception as e:
        print(f"{Colors.RED}HATA: {filepath} okunamadı: {e}{Colors.END}")
        return [], False

def save_json_file(filepath: Path, data: List[Dict]) -> bool:
    """JSON dosyasını kaydet"""
    try:
        with open(filepath, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=4)
        return True
    except Exception as e:
        print(f"{Colors.RED}HATA: {filepath} yazılamadı: {e}{Colors.END}")
        return False

def check_id_collision(all_ids: Dict[str, str], new_id: str, filepath: str) -> bool:
    """ID çakışması kontrolü"""
    if new_id in all_ids:
        print(f"{Colors.RED}❌ ID ÇAKIŞMASI: '{new_id}'")
        print(f"   Mevcut: {all_ids[new_id]}")
        print(f"   Yeni: {filepath}{Colors.END}")
        return True
    return False

def process_json_files(base_path: Path) -> Tuple[int, int, List[str], List[str]]:
    """Tüm JSON dosyalarını işle"""
    
    resources_path = base_path / "Resources" / "Raw"
    
    if not resources_path.exists():
        print(f"{Colors.RED}HATA: Resources/Raw dizini bulunamadı!{Colors.END}")
        return 0, 0, [], ["Resources/Raw dizini bulunamadı"]
    
    updated_files = []
    errors = []
    total_questions = 0
    ids_added = 0
    all_ids: Dict[str, str] = {}  # id -> filepath
    
    # Tüm JSON dosyalarını bul
    json_files = list(resources_path.glob("sorular_*.json"))
    
    print(f"\n{Colors.BLUE}📁 Bulunan JSON dosyaları: {len(json_files)}{Colors.END}\n")
    
    for filepath in sorted(json_files):
        filename = filepath.name
        category = get_category_from_filename(filename)
        exam_type = get_exam_type_from_filename(filename)
        
        print(f"📄 İşleniyor: {filename}")
        
        data, success = load_json_file(filepath)
        if not success:
            errors.append(f"{filename}: Dosya okunamadı")
            continue
        
        file_modified = False
        file_ids_added = 0
        
        for i, question in enumerate(data):
            total_questions += 1
            question_num = i + 1
            
            # ID kontrolü
            existing_id = question.get("id") or question.get("Id")
            
            if existing_id:
                # Mevcut ID - çakışma kontrolü
                if check_id_collision(all_ids, existing_id, str(filepath)):
                    errors.append(f"{filename}: ID çakışması - {existing_id}")
                    return 0, 0, [], errors  # DURDUR
                all_ids[existing_id] = str(filepath)
            else:
                # ID yok - yeni ID oluştur
                new_id = f"{category}_{exam_type}_q{question_num:03d}"
                
                # Çakışma kontrolü
                if check_id_collision(all_ids, new_id, str(filepath)):
                    errors.append(f"{filename}: Üretilen ID çakıştı - {new_id}")
                    return 0, 0, [], errors  # DURDUR
                
                question["id"] = new_id
                all_ids[new_id] = str(filepath)
                file_modified = True
                file_ids_added += 1
                ids_added += 1
        
        # Dosya değiştiyse kaydet
        if file_modified:
            if save_json_file(filepath, data):
                updated_files.append(filename)
                print(f"   {Colors.GREEN}✅ {file_ids_added} ID eklendi{Colors.END}")
            else:
                errors.append(f"{filename}: Yazma hatası")
        else:
            print(f"   {Colors.YELLOW}⏭ Zaten tüm ID'ler mevcut{Colors.END}")
    
    return total_questions, ids_added, updated_files, errors

def main():
    print("=" * 60)
    print("🔧 SRC SORU ID ATAMA ARACI")
    print("=" * 60)
    
    # Çalışma dizinini bul
    script_dir = Path(__file__).parent
    base_path = script_dir.parent  # tools dizininin üstü
    
    print(f"\n📂 Proje dizini: {base_path}")
    
    # İşlemi başlat
    total, added, updated, errors = process_json_files(base_path)
    
    # Rapor
    print("\n" + "=" * 60)
    print("📊 RAPOR")
    print("=" * 60)
    
    print(f"\n📌 Toplam soru: {total}")
    print(f"📌 Eklenen ID: {added}")
    print(f"📌 Güncellenen dosya: {len(updated)}")
    
    if updated:
        print(f"\n{Colors.GREEN}✅ Güncellenen dosyalar:{Colors.END}")
        for f in updated:
            print(f"   - {f}")
    
    if errors:
        print(f"\n{Colors.RED}❌ HATALAR:{Colors.END}")
        for e in errors:
            print(f"   - {e}")
        print(f"\n{Colors.RED}İŞLEM BAŞARISIZ!{Colors.END}")
        sys.exit(1)
    else:
        print(f"\n{Colors.GREEN}✅ İŞLEM BAŞARILI!{Colors.END}")
        
        if added > 0:
            print(f"\n{Colors.YELLOW}⚠️ ÖNEMLİ: {added} yeni ID eklendi.")
            print(f"   Lütfen değişiklikleri commit edin.{Colors.END}")
        else:
            print(f"\n{Colors.GREEN}Tüm sorularda zaten ID mevcut.{Colors.END}")

if __name__ == "__main__":
    main()
