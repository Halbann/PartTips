using KSP.UI.Screens.Editor;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

namespace PartTips
{
    static class TooltipPrefab
    {
        public static int padding = 9;

        public static PartListTooltip Create()
        {
            // Take the stock PartListTooltip prefab, clone it and modify it to our needs.

            // TODO:
            // - avoid using FindObjectsOfTypeAll or any magic strings.
            // - link the buttons in a better way when the game clones the prefab.

            // Find and clone the original.
            PartListTooltip original = Resources.FindObjectsOfTypeAll<PartListTooltip>().FirstOrDefault(t => !t.name.Contains("Clone"));
            PartListTooltip prefab = Instantiate(original);
            prefab.name = "PartTipsTooltip";
            DontDestroyOnLoad(prefab.gameObject);
            prefab.gameObject.AddComponent<TooltipState>();

            // Get the tooltip elements.
            GameObject standard = prefab.gameObject.GetChild("StandardInfo");
            GameObject extended = prefab.panelExtended;
            GameObject thumbAndPrimaryInfo = standard.gameObject.GetChild("ThumbAndPrimaryInfo");
            GameObject thumbContainer = thumbAndPrimaryInfo.gameObject.GetChild("ThumbContainer");

            // Disable the thumbnail.
            thumbContainer.SetActive(false);

            // Make the standard info section narrower by default.
            LayoutElement layoutElement = standard.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 230f;
            layoutElement.minWidth = 230f;

            // Create a custom HEADER.
            GameObject header = CreateHeader(prefab);

            // Create a SPACER between the header and the body.
            GameObject verticalSpacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            verticalSpacer.GetComponent<LayoutElement>().minHeight = 5;
            verticalSpacer.transform.SetParent(prefab.transform, false);

            // Create the BODY.
            GameObject body = new GameObject("Body", typeof(RectTransform), typeof(LayoutElement));
            HorizontalLayoutGroup bodyHori = body.AddComponent<HorizontalLayoutGroup>();
            body.transform.SetParent(prefab.transform, false);

            // Move the standard and extended info sections into the body.
            standard.transform.SetParent(body.transform, false);
            standard.GetComponent<VerticalLayoutGroup>().padding = new RectOffset();
            standard.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained; //
            extended.transform.SetParent(body.transform, false);
            extended.GetComponent<VerticalLayoutGroup>().padding = new RectOffset();

            // Create a SPACER in the body.
            GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.GetComponent<LayoutElement>().minWidth = 5;
            spacer.transform.SetParent(body.transform, false);
            spacer.SetActive(false);
            spacer.transform.SetSiblingIndex(1);

            // REARRANGE the order of the main elements.
            header.transform.SetSiblingIndex(0);
            verticalSpacer.transform.SetSiblingIndex(1);
            body.transform.SetSiblingIndex(2);

            // Destroy the prefab's old layout group.
            HorizontalLayoutGroup prefabHori = prefab.GetComponent<HorizontalLayoutGroup>();
            if (prefabHori != null)
            {
                prefabHori.enabled = false;
                DestroyImmediate(prefabHori);
            }

            // Add a new layout group to the prefab.
            VerticalLayoutGroup prefabVert = prefab.gameObject.AddComponent<VerticalLayoutGroup>();
            prefabVert.padding = new RectOffset(padding, padding, 7, padding);

            return prefab;
        }

        private static GameObject CreateHeader(PartListTooltip prefab)
        {
            // Create the HEADER.
            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement));
            HorizontalLayoutGroup headerHori = header.AddComponent<HorizontalLayoutGroup>();
            headerHori.childForceExpandWidth = false;
            headerHori.childForceExpandHeight = false;
            header.transform.SetParent(prefab.transform, false);


            // Move the part NAME onto the header.
            GameObject nameText = prefab.textName.gameObject;
            GameObject textHolder = nameText.transform.parent.gameObject;
            nameText.transform.SetParent(header.transform, false);
            Destroy(textHolder);
            nameText.AddComponent<LayoutElement>().minHeight = 14;
            prefab.textName.alignment = TMPro.TextAlignmentOptions.TopLeft;


            // Add a FIND in part list button.
            GameObject find = new GameObject("FindButton", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            find.transform.SetParent(header.transform, false);

            // Adjust the transform so that we can scale it down from the center.
            RectTransform findTransform = find.GetComponent<RectTransform>();
            findTransform.anchorMin = new Vector2(0.5f, 0.5f);
            findTransform.anchorMax = new Vector2(0.5f, 0.5f); // is this needed?

            // Find the image component and set the sprite.
            Image findImage = find.GetComponent<Image>();
            Texture2D searchIcon = GameDatabase.Instance.GetTexture("Tip-off/Icons/Search_Icon", false);
            findImage.sprite = Sprite.Create(searchIcon, new Rect(0, 0, searchIcon.width, searchIcon.height), new Vector2(0.5f, 0.5f));

            // Adjust the layout element. A little bit of bonus width acts as padding.
            LayoutElement findLayout = find.GetComponent<LayoutElement>();
            findLayout.preferredHeight = 18;
            findLayout.preferredWidth = 27;
            find.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;

            // Make the button yellow when selected and faded when not.
            Button findButton = find.GetComponent<Button>();
            ColorBlock findColours = findButton.colors;

            findColours.normalColor = new Color(1f, 1f, 1f, 0.5f);
            findColours.highlightedColor = new Color(1f, 1f, 1f, 1f);
            findColours.selectedColor = findColours.normalColor;

            Color pressedColour = PartTips.highlightColour;
            pressedColour.a = 1;
            findColours.pressedColor = pressedColour;

            findButton.colors = findColours;


            // Add CLOSE button to the header.

            Button deleteButton = Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b => b.name == "DeleteButton");
            Sprite deleteButtonSprite = deleteButton.GetComponent<Image>().sprite;
            SpriteState deleteButtonSpriteState = deleteButton.spriteState;

            GameObject close = new GameObject("CloseButton", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            close.transform.SetParent(header.transform, false);
            close.GetComponent<Image>().sprite = deleteButtonSprite;

            Button closeButton = close.GetComponent<Button>();
            closeButton.spriteState = deleteButtonSpriteState;
            closeButton.transition = Selectable.Transition.SpriteSwap;
            close.GetComponent<LayoutElement>().preferredHeight = 20;
            close.GetComponent<LayoutElement>().preferredWidth = 20;


            // Need button to be right aligned, so add a SPACER to the header.

            GameObject spacer2 = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer2.GetComponent<LayoutElement>().flexibleWidth = 1;
            spacer2.GetComponent<LayoutElement>().minWidth = 5;
            spacer2.transform.SetParent(header.transform, false);
            spacer2.transform.SetSiblingIndex(2);

            return header;
        }
    }
}
