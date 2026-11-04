import pandas as pd
import sys

file_path = r"C:\Users\ZAHID\Desktop\Complate\__Monthly Change Management Report-April'2026.xlsx"
try:
    df_dict = pd.read_excel(file_path, sheet_name=None, header=None)
    for name, df in df_dict.items():
        print(f"\nDetected Worksheet: {name}")
        print("First 5 rows:")
        for i in range(min(5, len(df))):
            row = df.iloc[i].fillna("").tolist()
            print(f"Row {i+1}: {row}")
except Exception as e:
    print(f"Error reading file: {e}")
