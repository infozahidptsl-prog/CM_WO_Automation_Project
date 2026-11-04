import re

with open('G:/Development/WO_Automation Project/Application/WO_CM_Review_Automation/WO_CM_Review_Automation.Web/Components/Pages/CMReview/DocumentPreparation.razor', 'r') as f:
    c = f.read()

c = c.replace('</EnterpriseCard>@if (IsDocumentLoaded)', '</div>\n                    </EnterpriseCard>\n\n                @if (IsDocumentLoaded)')
c = c.replace('</EnterpriseCard><EnterpriseCard Class="mb-6">', '</div>\n                    </EnterpriseCard>\n\n                    <EnterpriseCard Class="mb-6">')
c = c.replace('</EnterpriseCard><div class="ent-p-md">', '</div>\n                        </div>\n                        <div class="ent-p-md">')
c = c.replace('</EnterpriseCard></MudItem><!-- Right Column: Live Preview -->', '</div>\n                    </EnterpriseCard>\n            </MudItem>\n\n            <!-- Right Column: Live Preview -->')

with open('G:/Development/WO_Automation Project/Application/WO_CM_Review_Automation/WO_CM_Review_Automation.Web/Components/Pages/CMReview/DocumentPreparation.razor', 'w') as f:
    f.write(c)
