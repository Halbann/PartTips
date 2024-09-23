using KSP.Localization;
using KSP.UI;
using KSP.UI.Screens;
using KSP.UI.Screens.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PartTips
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class PartTips : MonoBehaviour
    {
        public static PartTips Instance { get; private set; }
        public readonly static GameScenes[] validScenes = { GameScenes.FLIGHT, GameScenes.EDITOR };

        public Part CurrentPart { get; private set; }
        public PartListTooltipController tooltipController;
        public static PartListTooltip prefab;
        public AvailablePart dummyPartInfo = new AvailablePart();
        private bool buttonsLinked = false;
        
        private float mmbDownTime = 0f;
        private float rmbDownTime = 0f;
        public static float mbClickDuration = 0.2f;

        public static int highlightPulses = 3;
        public static float highlightSpeed = 9f;
        public static bool highlightSpin = false;
        public static Color highlightColour = new Color(1f, 0.82f, 0f, 0.66f); // yellow
        // Color(0.68f, 1f, 0.98f, 0.66f); // blue

        // problems:
        // - localisation
        // - get stock assets without using FindObjectsOfTypeAll

        protected void Start()
        {
            if (!validScenes.Contains(HighLogic.LoadedScene))
            {
                Destroy(this);
                return;
            }

            Instance = this;

            // Create the altered tooltip prefab (once).
            if (prefab == null)
                prefab = TooltipPrefab.Create();

            // Add stock tooltip components.
            tooltipController = gameObject.AddComponent<PartListTooltipController>();
            tooltipController.editorPartIcon = gameObject.AddComponent<EditorPartIcon>();
            tooltipController.prefabType = prefab;

            // We need to add the PartListTooltipMasterController to the flight scene, as it's not there by default.
            if (HighLogic.LoadedSceneIsFlight)
                gameObject.AddComponent<PartListTooltipMasterController>();
        }

        protected void Update()
        {
            CheckInput();
        }

        private void CheckInput()
        {
            if (HighLogic.LoadedSceneIsEditor && InputLockManager.IsLocked(ControlTypes.EDITOR_PAD_PICK_PLACE))
                return;

            if (HighLogic.LoadedSceneIsFlight && InputLockManager.IsLocked(ControlTypes.CAMERACONTROLS) || InputLockManager.IsLocked(ControlTypes.TWEAKABLES_ANYCONTROL))
                return;

            if (Input.GetMouseButtonDown(2))
                mmbDownTime = Time.realtimeSinceStartup;

            if (Input.GetMouseButtonDown(1))
                rmbDownTime = Time.realtimeSinceStartup;

            if (TooltipState.open && ((Input.GetMouseButtonUp(1) && Time.realtimeSinceStartup - rmbDownTime < mbClickDuration) || CurrentPart == null))
            {
                // Right click empty space to close the tooltip.

                Close();
                return;
            }

            if (Input.GetMouseButtonUp(2) && Time.realtimeSinceStartup - mmbDownTime < mbClickDuration)
            {
                // MMB click.

                if (HighLogic.LoadedSceneIsEditor && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Mouse.HoveredPart != null)
                {
                    // Alt + MMB Click: flash highlight and find part in part list
                    StartCoroutine(HighlightInPartList(Mouse.HoveredPart.partInfo));
                    return;
                }

                if (TooltipState.open)
                {
                    if (Mouse.HoveredPart == CurrentPart && CurrentPart != null)
                    {
                        // Toggle extended info MMB clicking the part.
                        SetExtendedInfo(!PartListTooltipMasterController.Instance.displayExtendedInfo);

                        return;
                    }
                    else
                    {
                        // Close tooltip by MMB clicking empty space.
                        Close();
                    }
                }

                if (Mouse.HoveredPart == null || Mouse.HoveredPart == CurrentPart)
                    return;

                // Open tooltip.
                Open(Mouse.HoveredPart);
            }
        }

        public void Open(Part part)
        {
            if (prefab == null)
            {
                Debug.LogError("[PartTips] Error: Tooltip prefab is missing.");
                return;
            }

            CurrentPart = part;

            // Create a new AvailablePart from the current part.
            // We will modify this to reflect the current state of that part (resources, cost, modules etc).

            dummyPartInfo = new AvailablePart(part.partInfo);
            dummyPartInfo.moduleInfos = new List<AvailablePart.ModuleInfo>();
            dummyPartInfo.resourceInfos = new List<AvailablePart.ResourceInfo>();

            // Cost.
            float offsetCost = GetOffsetPartCost(part);
            dummyPartInfo.cost = offsetCost;
            dummyPartInfo.minimumCost = 0;

            // Modules.
            PartLoader.Instance.CompilePartInfo(dummyPartInfo, part);

            // Resources.
            RecompileResourceInfos(part, dummyPartInfo);

            // Assign the instance info to our dummy icon held by our tooltip.
            dummyPartInfo.partPrefab = part;
            tooltipController.editorPartIcon.availablePart = dummyPartInfo;

            // Open and pin the tooltip.
            tooltipController.OnPointerEnter(null);
            UIMasterController.Instance.PinTooltip(tooltipController);
            tooltipController.pinned = true;

            // Highlight the part in yellow.
            SetHighlight(part, true);

            // Manually display the extended info in flight if it's supposed to be visible.
            PartListTooltipMasterController master = PartListTooltipMasterController.Instance;
            PartListTooltip tooltip = master.currentTooltip;
            if (!master.useRenderTextureCamera)
                tooltip.DisplayExtendedInfo(master.displayExtendedInfo, tooltipController.GetTooltipHintText(tooltip));

            // Update the custom spacer between the standard and extended info.
            tooltip.gameObject.GetChild("Body")?.GetChild("Spacer")?.SetActive(master.displayExtendedInfo);

            // Update the find icon depending on scene.
            tooltip.gameObject.GetChild("Header")?.GetChild("FindButton")?.SetActive(HighLogic.LoadedSceneIsEditor);

            // Override the dynamic less/more hint from RMB to MMB.
            string original = tooltip.textRMBHint.text;
            tooltip.textRMBHint.text = ModifyHintText(original);

            // Link the buttons on the cloned tooltip instance (once).
            if (!buttonsLinked)
            {
                LinkButton(tooltip.gameObject);
                buttonsLinked = true;
            }
        }

        public void Close()
        {
            if (CurrentPart != null)
                SetHighlight(CurrentPart, false);

            if (tooltipController.tooltipInstance != null && tooltipController.tooltipInstance.gameObject.activeInHierarchy)
                tooltipController.OnDestroy();

            CurrentPart = null;
            TooltipState.open = false;
        }

        public void SetExtendedInfo(bool toggle)
        {
            PartListTooltipMasterController tooltipMaster = PartListTooltipMasterController.Instance;

            // Change displayExtendedInfo state.
            tooltipMaster.displayExtendedInfo = toggle;

            if (tooltipMaster.currentTooltip == null || tooltipController == null)
                return;

            // Update tooltip object.
            string tooltipHintText = tooltipController.GetTooltipHintText(tooltipMaster.currentTooltip);
            tooltipMaster.currentTooltip.DisplayExtendedInfo(display: toggle, ModifyHintText(tooltipHintText));
            tooltipMaster.currentTooltip.gameObject.GetChild("Body")?.GetChild("Spacer")?.SetActive(toggle);
        }

        private void LinkButton(GameObject tooltip)
        {
            tooltip.GetChild("Header")?.GetChild("CloseButton")?.GetComponent<Button>().onClick.AddListener(Close);
            tooltip.GetChild("Header")?.GetChild("FindButton")?.GetComponent<Button>().onClick.AddListener(() => StartCoroutine(HighlightInPartList(CurrentPart?.partInfo)));
        }

        private string ModifyHintText(string original)
        {
            // TODO: proper localisation.

            string modified = original.Replace("RMB", "MMB");
            modified = modified.Replace("Pin, Less", "Less Info");

            return modified;
        }

        private void SetHighlight(Part part, bool on)
        {
            if (part == null)
                return;

            part.SetHighlightDefault();

            if (on)
            {
                part.SetHighlightType(Part.HighlightType.AlwaysOn);
                part.SetHighlight(active: true, recursive: false);
                part.SetHighlightColor(highlightColour);
            }
        }

        public IEnumerator HighlightInPartList(AvailablePart partInfo)
        {
            // In the editor, switch to the correct category and highlight the given part.

            if (!HighLogic.LoadedSceneIsEditor || partInfo == null)
                yield break;

            // Find the category that contains the part.
            PartCategorizer.Category category = PartCategorizer.Instance.filterFunction.subcategories.FirstOrDefault(c => c.exclusionFilter.FilterCriteria(partInfo));
            if (category == null)
            {
                Debug.LogError($"[PartTips] Error: subcategory not found for {partInfo.title} {partInfo.partUrl}.");
                yield break;
            }

            // Spoof a click.
            PointerEventData leftClick = new PointerEventData(EventSystem.current);
            leftClick.button = PointerEventData.InputButton.Left;

            // Click on the filter by function button.
            UIRadioButton functionButton = PartCategorizer.Instance.filterFunction.button.btnToggleGeneric;
            if (functionButton.CurrentState == UIRadioButton.State.False)
                functionButton.SetState(UIRadioButton.State.True, UIRadioButton.CallType.USER, leftClick);

            // Click on the category button.
            bool switchCategories = category.button.btnToggleGeneric.CurrentState == UIRadioButton.State.False;
            if (switchCategories)
                category.button.btnToggleGeneric.SetState(UIRadioButton.State.True, UIRadioButton.CallType.USER, leftClick);

            // Wait for the category to switch.
            yield return new WaitUntil(() => !PartCategorizer.Instance.refreshRequested);

            // Find the icon of the part.
            EditorPartIcon icon = EditorPartList.Instance.icons.FirstOrDefault(i => i.partInfo.name == partInfo.name);
            if (icon == null)
            {
                Debug.LogError($"[PartTips] Error: Editor icon not found for {partInfo.title} {partInfo.partUrl}.");
                yield break;
            }

            // Activate the highlight in the background icon.
            EventSystem.current.SetSelectedGameObject(icon.gameObject);
            StartCoroutine(AnimatedHighlight(icon));

            // Scroll to the icon if it's not visible.

            // Check if the icon is within the scroll rect.
            ScrollRect scrollRect = EditorPartList.Instance.partListScrollRect;
            Vector3 screenPos = UIMainCamera.Instance.cam.WorldToScreenPoint(icon.transform.position);
            bool visible = RectTransformUtility.RectangleContainsScreenPoint(scrollRect.transform as RectTransform, screenPos, UIMainCamera.Instance.cam);

            if (!visible || switchCategories)
            {
                // Sum the displacement from the scroll starting position through the hierarchy (compatibility ith VABOrganizer).

                float displacement = 0;
                Transform transform = icon.transform;

                while (transform.parent.gameObject.GetComponent<ScrollRect>() == null)
                {
                    displacement += (transform as RectTransform).anchoredPosition.y;
                    transform = transform.parent;
                }

                Vector2 pos = scrollRect.content.anchoredPosition;
                float offset = scrollRect.content.sizeDelta.y / 2; // Some tasteful offset.
                pos.y = -displacement - offset;
                scrollRect.content.anchoredPosition = pos; // Move the scroll bar.

                // Save the scrollbar position for this category, otherwise it will be set back a few frames later.
                string scrollbarKey = EditorPartList.Instance.CategorizerFilters.GetFilterKeySingleOrNothing();
                EditorPartList.Instance.scrollbarPositions[scrollbarKey] = scrollRect.verticalNormalizedPosition;
            }
        }

        IEnumerator AnimatedHighlight(EditorPartIcon icon)
        {
            // Flashing yellow box around the given icon.

            Image highlight = icon.gameObject.GetChild("Highlight")?.GetComponent<Image>();
            if (highlight == null || highlight.gameObject.activeSelf)
                yield break;

            highlight.gameObject.SetActive(true);
            Color originalColour = highlight.color;
            Color animatedColour = highlightColour;
            float startTime = Time.unscaledTime;
            float endTime = Mathf.PI * 2 * highlightPulses;

            if (highlightSpin)
                icon.Highlight();

            while (true)
            {
                float t = (Time.unscaledTime - startTime) * highlightSpeed;
                animatedColour.a = Mathf.Sin(t - Mathf.PI / 2) * 0.5f + 0.5f;
                highlight.color = animatedColour;

                if (t > endTime)
                    break;

                yield return null;
                if (highlight == null)
                    yield break;
            }

            if (highlightSpin)
                icon.Unhighlight();

            highlight.gameObject.SetActive(false);
            highlight.color = originalColour;
        }

        public static float GetOffsetPartCost(Part part)
        {
            // Get the dry cost of the part, plus the cost of the CURRENT resources, minus the cost of the modules.

            // We offset by the cost of the modules because that's added later by the game.
            // We are basically just doing this to sneak the cost of the current resources into the final number.
            // Normally the game just displays the cost of the prefab, which doesn't change with resources.

            float total = 0f;
            float dryCost = 0f;
            float fuelCost = 0f;

            AvailablePart partInfo = part.partInfo;
            float moduleCost = part.GetModuleCosts(partInfo.cost);
            float num2 = partInfo.cost + moduleCost;

            float num3 = 0f;
            int count2 = part.Resources.Count;
            while (count2-- > 0)
            {
                PartResource partResource = part.Resources[count2];
                PartResourceDefinition info = partResource.info;
                num2 -= info.unitCost * (float)partResource.maxAmount;
                num3 += info.unitCost * (float)partResource.amount;
            }

            dryCost += num2;
            fuelCost += num3;
            total += dryCost + fuelCost;

            return total - moduleCost;
        }

        public static void RecompileResourceInfos(Part part, AvailablePart partInfo)
        {
            // Recompile the resource infos, specifically the 'primaryInfo' field,
            // everytime the tooltip is opened, because the amount can change.

            partInfo.resourceInfos.Clear();

            int j = 0;
            for (int count2 = part.Resources.Count; j < count2; j++)
            {
                PartResource partResource = part.Resources[j];

                AvailablePart.ResourceInfo resourceInfo = new AvailablePart.ResourceInfo
                {
                    resourceName = partResource.resourceName,
                    displayName = partResource.info.displayName.LocalizeRemoveGender(),
                    info = Localizer.Format("#autoLOC_166269", partResource.amount.ToString("F1")) + ((partResource.amount != partResource.maxAmount) ? (" " + Localizer.Format("#autoLOC_6004042", partResource.maxAmount.ToString("F1"))) : "") + Localizer.Format("#autoLOC_166270", (partResource.amount * (double)partResource.info.density).ToString("F2")) + Localizer.Format("#autoLOC_7001407") + ((partResource.info.unitCost > 0f) ? Localizer.Format("#autoLOC_166271", (partResource.amount * (double)partResource.info.unitCost).ToString("F2")) : "")
                };

                if (partResource.maxAmount > 0.0)
                    resourceInfo.primaryInfo = "<b>" + resourceInfo.displayName + ": </b>" + KSPUtil.LocalizeNumber(partResource.amount, "F1") + " / " + KSPUtil.LocalizeNumber(partResource.maxAmount, "F1");

                if (!string.IsNullOrEmpty(resourceInfo.info))
                    partInfo.resourceInfos.Add(resourceInfo);
            }

            partInfo.resourceInfos.Sort((AvailablePart.ResourceInfo rp1, AvailablePart.ResourceInfo rp2) => rp1.resourceName.CompareTo(rp2.resourceName));
        }
    }
}
