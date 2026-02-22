#!/usr/bin/env python3
"""
String Encryption Tool for ECoopSystem
Encrypts strings using AES-256-CBC matching the C# StringEncryption class
"""

import sys
import base64
import hashlib
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.backends import default_backend
from cryptography.hazmat.primitives import padding

def derive_key():
    """Derive encryption key - must match C# implementation"""
    data = "ECoopSystem.SecureKey.2026.LandsHorizon.v1"
    return hashlib.sha256(data.encode('utf-8')).digest()

def derive_iv():
    """Derive initialization vector - must match C# implementation"""
    data = "ECoopSystem.IV.2026.SecureInit"
    hash_value = hashlib.sha256(data.encode('utf-8')).digest()
    return hash_value[:16]

def encrypt_string(plain_text):
    """Encrypt a string using AES-256-CBC"""
    key = derive_key()
    iv = derive_iv()
    
    # Add PKCS7 padding
    padder = padding.PKCS7(128).padder()
    padded_data = padder.update(plain_text.encode('utf-8')) + padder.finalize()
    
    # Encrypt
    cipher = Cipher(algorithms.AES(key), modes.CBC(iv), backend=default_backend())
    encryptor = cipher.encryptor()
    encrypted = encryptor.update(padded_data) + encryptor.finalize()
    
    return base64.b64encode(encrypted).decode('utf-8')

def decrypt_string(encrypted_base64):
    """Decrypt a string using AES-256-CBC"""
    key = derive_key()
    iv = derive_iv()
    
    encrypted = base64.b64decode(encrypted_base64)
    
    # Decrypt
    cipher = Cipher(algorithms.AES(key), modes.CBC(iv), backend=default_backend())
    decryptor = cipher.decryptor()
    decrypted_padded = decryptor.update(encrypted) + decryptor.finalize()
    
    # Remove PKCS7 padding
    unpadder = padding.PKCS7(128).unpadder()
    decrypted = unpadder.update(decrypted_padded) + unpadder.finalize()
    
    return decrypted.decode('utf-8')

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python encrypt-string.py <text_to_encrypt>")
        print("   or: python encrypt-string.py --decrypt <encrypted_base64>")
        sys.exit(1)
    
    if sys.argv[1] == "--decrypt":
        if len(sys.argv) < 3:
            print("Error: No encrypted text provided")
            sys.exit(1)
        encrypted = sys.argv[2]
        try:
            decrypted = decrypt_string(encrypted)
            print(f"\nDecrypted: {decrypted}")
        except Exception as e:
            print(f"Error decrypting: {e}")
            sys.exit(1)
    else:
        plain_text = sys.argv[1]
        encrypted = encrypt_string(plain_text)
        
        print(f"\nOriginal: {plain_text}")
        print(f"Encrypted: {encrypted}")
        print(f"\nUse in C# code:")
        print(f'StringEncryption.Decrypt("{encrypted}")')
        print("")
