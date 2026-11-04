import asyncio
from playwright.async_api import async_playwright
import os

async def run():
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        page = await browser.new_page(viewport={"width": 1920, "height": 1080})
        
        print("Navigating to Dashboard...")
        await page.goto("http://localhost:5052/")
        await page.wait_for_selector("text=Operational Dashboard", timeout=10000)
        await page.screenshot(path="E:/Development/WO_CM_Review_Automation/TestFiles/Screenshots/Audit_01_Dashboard.png")
        
        print("Clicking on a drill-down link...")
        drilldown_links = await page.query_selector_all("td >> a")
        if drilldown_links:
            await drilldown_links[0].click()
            await page.wait_for_selector(".mud-dialog", timeout=5000)
            await page.screenshot(path="E:/Development/WO_CM_Review_Automation/TestFiles/Screenshots/Audit_02_Drilldown.png")
            
            # Close dialog by clicking close button or pressing Escape
            await page.keyboard.press("Escape")
            await page.wait_for_timeout(1000)
        
        print("Navigating to WO Master List...")
        await page.goto("http://localhost:5052/wo-list")
        await page.wait_for_selector("text=Work Order Master List", timeout=10000)
        await page.screenshot(path="E:/Development/WO_CM_Review_Automation/TestFiles/Screenshots/Audit_03_MasterList.png")
        
        print("Navigating to Monthly Reports...")
        await page.goto("http://localhost:5052/report")
        await page.wait_for_selector("text=Report Viewer", timeout=10000)
        await page.screenshot(path="E:/Development/WO_CM_Review_Automation/TestFiles/Screenshots/Audit_04_Report.png")
        
        print("Workflow complete.")
        await browser.close()

if __name__ == "__main__":
    asyncio.run(run())
