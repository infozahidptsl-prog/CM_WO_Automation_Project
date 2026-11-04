from playwright.sync_api import sync_playwright
import time
import os

workspace = r"E:\Development\WO_CM_Review_Automation"
screenshots_dir = os.path.join(workspace, "TestFiles", "Screenshots")
os.makedirs(screenshots_dir, exist_ok=True)

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 1920, "height": 1080})
    
    # 1. Login/Dashboard (Assuming no actual login required for dev)
    print("Navigating to Dashboard...")
    page.goto("http://localhost:5052/")
    time.sleep(2)
    page.screenshot(path=os.path.join(screenshots_dir, "01_Dashboard.png"))
    
    # 2. Upload Page
    print("Navigating to Upload Page...")
    page.goto("http://localhost:5052/upload")
    time.sleep(1)
    page.screenshot(path=os.path.join(screenshots_dir, "02_UploadPage.png"))
    
    # 3. Upload File
    print("Uploading file...")
    file_path = os.path.join(workspace, "TestFiles", "Correct_Excel.xlsx")
    page.set_input_files("input[type='file']", file_path)
    time.sleep(2)
    page.screenshot(path=os.path.join(screenshots_dir, "03_FileSelected.png"))
    
    # 4. Generate Preview
    print("Generating Preview...")
    page.click("button:has-text('Generate Preview')")
    time.sleep(5)
    page.screenshot(path=os.path.join(screenshots_dir, "04_PreviewGenerated.png"))
    
    # 5. Confirm Import
    print("Confirming Import...")
    page.click("button:has-text('Confirm Import')")
    time.sleep(10) # wait for backend processing
    page.screenshot(path=os.path.join(screenshots_dir, "05_ImportSummary.png"))
    
    # 6. Dashboard again
    print("Going back to Dashboard...")
    page.goto("http://localhost:5052/")
    time.sleep(3)
    page.screenshot(path=os.path.join(screenshots_dir, "06_Dashboard_AfterImport.png"))
    
    browser.close()
    print("Workflow complete.")
