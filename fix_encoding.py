# -*- coding: utf-8 -*-
import os
import re

# Character replacements (mojibake to correct Turkish)
# UTF-8 bytes interpreted as Latin-1
replacements = {
    'Ä±': 'ı',  # ı
    'ÅŸ': 'ş',  # ş
    'ÄŸ': 'ğ',  # ğ
    'Ã¼': 'ü',  # ü
    'Ã¶': 'ö',  # ö
    'Ã§': 'ç',  # ç
    'Ä°': 'İ',  # İ
    'Åž': 'Ş',  # Ş
    'Äž': 'Ğ',  # Ğ
    'Ãœ': 'Ü',  # Ü
    'Ã–': 'Ö',  # Ö
    'Ã‡': 'Ç',  # Ç
    'Ã¢': 'â',  # â
    'â€"': '–', # en dash
    'â€œ': '"', # left quote
    'â€': '"',  # right quote
    "â€™": "'", # apostrophe
    'Ã‰': 'É',  # É
    'Ã©': 'é',  # é
    'Ã': 'İ',   # İ (sometimes)
    'Ã½': 'ı',  # alternate ı
    'Ã': 'A',   # A with tilde fallback
}

files = [
    'Resources/Raw/sorular_src1.json',
    'Resources/Raw/sorular_src1_sinav.json', 
    'Resources/Raw/sorular_src1_resimli.json',
    'Resources/Raw/sorular_src2.json',
    'Resources/Raw/sorular_src2_sinav.json',
    'Resources/Raw/sorular_src2_resimli.json',
    'Resources/Raw/sorular_src3.json',
    'Resources/Raw/sorular_src3_sinav.json',
    'Resources/Raw/sorular_src3_resimli.json',
    'Resources/Raw/sorular_src4.json',
    'Resources/Raw/sorular_src4_sinav.json',
    'Resources/Raw/sorular_src4_resimli.json',
    'Resources/Raw/sorular_src5.json',
    'Resources/Raw/sorular_src5_sinav.json',
    'Resources/Raw/sorular_src5_resimli.json',
]

for filepath in files:
    if os.path.exists(filepath):
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
            
            original = content
            for wrong, correct in replacements.items():
                content = content.replace(wrong, correct)
            
            if content != original:
                with open(filepath, 'w', encoding='utf-8-sig') as f:
                    f.write(content)
                print(f'Fixed: {filepath}')
            else:
                print(f'No changes needed: {filepath}')
        except Exception as e:
            print(f'Error processing {filepath}: {e}')
    else:
        print(f'Not found: {filepath}')

print("Done!")
