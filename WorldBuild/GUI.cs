using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UITools;
using SFS;
using SFS.Parts;
using SFS.Builds;
using SFS.Career;
using SFS.UI.ModGUI;
using SFS.Parts.Modules;
using Button = SFS.UI.Button;
using static SFS.Builds.PickGridUI;
using ModButton = SFS.UI.ModGUI.Button;
using SFS.Input;

namespace WorldBuild.GUI
{
    public static class PartPickerUI
    {
        public static Transform GUIHolder;
        public static readonly Vector2Int size_main = new Vector2Int(400, 1000);
        public static readonly Vector2Int size_categories = new Vector2Int(240, 950);
        public static readonly Vector2Int size_parts = new Vector2Int(140, 950);
        public static readonly int id_main = Builder.GetRandomID();
        public static readonly int id_categories = Builder.GetRandomID();
        public static readonly int id_parts = Builder.GetRandomID();
        public static ClosableWindow window_main;
        public static Window window_categories;
        public static Window window_parts;
        public static ModButton button_selectedCategory = null;

        public static CategoryParts[] pickCategories = null;
        public static CategoryParts selectedCategory = null;
        /// <summary>
        /// Pseudo-mirror of <c>BuildManager.main.pickGrid.categoryOrder</c>
        /// </summary>
        public static List<PickCategory> categoryOrder = new List<PickCategory>();
        // {
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Basic_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Six_Wide_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Eight_Wide_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Ten_Wide_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Twelve_Wide_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Engine_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Aerodynamics_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Fairings_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Structural_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Loc.main.Other_Parts) },
        //     new PickCategory() { displayName = new TranslationVariable(Field.Text("Redstone Atlas")) },
        // };
        public static Transform createdPartsHolder;
        public static Dictionary<VariantRef, Part> createdParts = new Dictionary<VariantRef, Part>();

        public static void CreateUI()
        {
            if (pickCategories == null)
            {
                pickCategories = GetPickCategories();
                selectedCategory = pickCategories[0];
            }

            if (createdPartsHolder == null)
            {
                createdPartsHolder = new GameObject("World Build: Created Parts Holder").transform;
                Object.DontDestroyOnLoad(createdPartsHolder.gameObject);
            }

            DestroyUI();

            GUIHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "WorldBuild: UI Holder").transform;
            window_main = UIToolsBuilder.CreateClosableWindow
            (
                GUIHolder,
                id_main,
                size_main.x,
                size_main.y,
                draggable: true,
                opacity: 0.5f,
                titleText: "World Build"
            );
            window_main.RegisterPermanentSaving($"{Main.main.ModNameID}.pickgrid");
            window_main.CreateLayoutGroup(Type.Horizontal, TextAnchor.UpperCenter, 5f, new RectOffset(5, 5, 5, 5));
            CreateCategoriesUI();
            CreatePartsUI();
        }

        static void CreateCategoriesUI()
        {
            if (window_categories != null)
            {
                Object.Destroy(window_categories.gameObject);
            }
            window_categories = Builder.CreateWindow
            (
                window_main,
                id_categories,
                size_categories.x,
                size_categories.y,
                savePosition: false,
                opacity: 0.5f
            );
            window_categories.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter, 10f, new RectOffset(5, 5, 5, 5));
            window_categories.EnableScrolling(Type.Vertical);


            foreach (CategoryParts category in pickCategories)
            {
                ModButton button = null;
                button = Builder.CreateButton
                (
                    window_categories,
                    size_categories.x - 15,
                    size_categories.y / 20,
                    onClick: () =>
                    {
                        if (selectedCategory != category)
                        {
                            button_selectedCategory.SetSelected(false);
                            selectedCategory = category;
                            button_selectedCategory = button;
                            button_selectedCategory.SetSelected(true);
                            CreatePartsUI();
                        }
                    },
                    text: category.tag.displayName.Field
                );
                if (selectedCategory == category)
                {
                    button_selectedCategory = button;
                    button_selectedCategory.SetSelected(true);
                }

            }
        }

        static void CreatePartsUI()
        {
            if (window_parts != null)
            {
                Object.Destroy(window_parts.gameObject);
            }
            window_parts = Builder.CreateWindow
            (
                window_main,
                id_parts,
                size_parts.x,
                size_parts.y,
                savePosition: false,
                opacity: 0.5f
            );
            window_parts.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter, 10f, new RectOffset(5, 5, 5, 5));
            window_parts.EnableScrolling(Type.Vertical);

            foreach ((bool owned, VariantRef variant) in selectedCategory.parts)
            {
                if (owned)
                {
                    if (!createdParts.TryGetValue(variant, out Part part) || part == null)
                    {
                        part = PartsLoader.CreatePart(variant, true);
                        part.transform.parent = createdPartsHolder;
                        part.gameObject.SetActive(false);
                        createdParts.Add(variant, part);
                    }
                    Button button = CreatePartIcon(window_parts, part);
                    button.onHold += (OnInputStayData data) =>
                    {
                        if (data.inputType == InputType.MouseLeft)
                            Manager.main.CreateNewPart(variant, data.position.World(0f));
                    };
                    button.onClick += () => Debug.Log("TODO: Part info box.");
                    button.onRightClick += () => Debug.Log("TODO: Part info box.");
                }
            }
        }

        public static void DestroyUI()
        {
            if (GUIHolder != null)
                Object.Destroy(GUIHolder.gameObject);
        }

        public static void DestroyCreatedParts()
        {
            createdParts.Clear();
            if (createdPartsHolder != null)
                Object.Destroy(createdPartsHolder.gameObject);
        }

        // ? Derived from `SFS.Builds.PickGridUI.Initialize`.
        static CategoryParts[] GetPickCategories()
        {
            Dictionary<PickCategory, CategoryParts> dictionary = new Dictionary<PickCategory, CategoryParts>();
            foreach (VariantRef value in Base.partsLoader.partVariants.Values)
            {
                Part part = PartsLoader.CreatePart(value, updateAdaptation: true);
                bool item = part.GetOwnershipState() == OwnershipState.OwnedAndUnlocked && CareerState.main.HasPart(value);
                Object.DestroyImmediate(part.gameObject);
                foreach (Variants.PickTag pickTag in value.GetPickTags())
                {
                    if (pickTag.tag == null)
                    {
                        throw new System.Exception(value.part.name);
                    }
                    if (!categoryOrder.Contains(pickTag.tag))
                    {
                        categoryOrder.Add(pickTag.tag);
                    }
                    if (!dictionary.ContainsKey(pickTag.tag))
                    {
                        dictionary[pickTag.tag] = new CategoryParts(pickTag.tag);
                    }
                    dictionary[pickTag.tag].parts.Add((item, value));
                }
            }
            dictionary = dictionary.Where((KeyValuePair<PickCategory, CategoryParts> pair) => pair.Value.parts.Any(((bool owned, VariantRef part) a) => a.owned)).ToDictionary((KeyValuePair<PickCategory, CategoryParts> pair) => pair.Key, (KeyValuePair<PickCategory, CategoryParts> pair) => pair.Value);
            foreach (PickCategory category in dictionary.Keys)
            {
                dictionary[category].parts = dictionary[category].parts.OrderBy(((bool owned, VariantRef part) variant) => -variant.part.GetPriority(category)).ToList();
            }
            return dictionary.Values.OrderBy((CategoryParts picklist) => categoryOrder.IndexOf(picklist.tag)).ToArray();
        }

        static Button CreatePartIcon(Transform holder, Part part)
        {
            GameObject go = new GameObject
            (
                $"World Build: Part Icon ({part.name})",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage)
            );
            go.transform.SetParent(holder, false);

            Button button = go.AddComponent<Button>();
            button.clickEvent = new SFS.UI.ClickUnityEvent();
            button.holdEvent = new SFS.UI.HoldUnityEvent();

            RawImage img = go.GetComponent<RawImage>();
            part.gameObject.SetActive(true);
            img.texture = PartIconCreator.main.CreatePartIcon_PickGrid(part, out Vector2 size);
            part.gameObject.SetActive(false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.rect.width * (size.y / size.x));

            return button;
        }
    }
}