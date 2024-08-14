using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SFS.Input;
using SFS.World;
using SFS.Parts;
using SFS.Parts.Modules;
using WorldBuild.GUI;
using SFS.Cameras;

namespace WorldBuild
{
    public static class Manager
    {
        public static Part heldPart;
        public static List<Collider2D> disabledColliders = new List<Collider2D>();
        public static Rocket closestRocket;
        public static bool worldBuildActive;
        public static bool draggingPart;
        public static Vector2 partTargetPos;

        public static void EnterBuild()
        {
            worldBuildActive = true;
            PartPickerUI.CreateUI();
        }

        public static void ExitBuild()
        {
            draggingPart = false;
            worldBuildActive = false;
            PartPickerUI.DestroyUI();
            DestroyHeldPart();
        }

        public static void ToggleBuild()
        {
            if (PartPickerUI.GUIHolder == null)
            {
                EnterBuild();
            } else {
                ExitBuild();
            }
        }

        public static void CreateNewPart(VariantRef variant, Vector2 mousePos)
        {
            DestroyHeldPart();
            
            heldPart = PartsLoader.CreatePart(variant, true);

            heldPart.transform.position = mousePos;
            partTargetPos = mousePos;
            draggingPart = true;
            OnPartTransform();

            foreach (Collider2D col in heldPart.GetModules<Collider2D>())
            {
                if (col.isActiveAndEnabled && !col.isTrigger)
                {
                    disabledColliders.Add(col);
                    col.enabled = false;
                }
            }
        }

        public static void OnPartTransform()
        {
            // * Update closest rocket.
            float bestDist = 0f;
            int? bestIdx = null;
            for (int idx = 0; idx < GameManager.main.rockets.Count; idx++)
            {
                Rocket rocket = GameManager.main.rockets[idx];
                if (rocket.physics.PhysicsMode)
                {
                    float maxDist = 1.5f * rocket.GetSizeRadius();
                    float currentDist = (partTargetPos - rocket.rb2d.position).magnitude;
                    float dist = currentDist - maxDist;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx = idx;
                    }
                }
            }
            closestRocket = bestIdx is int i ? GameManager.main.rockets[i] : null;

            // * Update part rotation.
            float angle = closestRocket?.rb2d.rotation ?? ((float) WorldView.ToGlobalPosition(heldPart.transform.position).AngleDegrees - 90f);
            heldPart.transform.rotation = Quaternion.Euler(0, 0, angle);

            // * Update part position.
            Vector2 pos = partTargetPos;
            if (closestRocket != null)
            {
                Vector2 localPos = closestRocket.partHolder.transform.InverseTransformPoint(pos);
                pos = closestRocket.partHolder.transform.TransformPoint(localPos.Round(0.5f));
            }
            heldPart.transform.position = pos;
        }

        public static void DestroyHeldPart()
        {
            disabledColliders.Clear();
            try { heldPart.DestroyPart(false, false, DestructionReason.Intentional); } catch (NullReferenceException) { }
        }

        public static void AddInputs()
        {
            Screen_Game input = GameManager.main.world_Input;
            input.onInputStart += OnInputStart;
            input.onInputEnd += OnInputEnd;
            input.onDrag += OnDrag;

            ActiveCamera.Camera.position.OnChange += OnCameraPositionChange;
        }

        public static void OnInputStart(OnInputStartData data)
        {
            if (data.inputType == InputType.MouseLeft && heldPart != null)
            {
                Vector2 pos = data.position.World(0f);
                draggingPart = Part_Utility.RaycastParts(new [] { heldPart }, pos, 0.3f, out PartHit _);
            }
        }

        public static void OnInputEnd(OnInputEndData data)
        {
            if (data.LeftClick && heldPart != null)
            {
                draggingPart = false;
            }
        }

        public static void OnDrag(DragData data)
        {
            if (draggingPart && heldPart != null)
            {
                partTargetPos -= data.DeltaWorld(0f);
                OnPartTransform();
            }
        }

        public static void OnCameraPositionChange(Vector2 oldPos, Vector2 newPos)
        {
            if (heldPart != null)
            {
                partTargetPos += newPos - oldPos;
                OnPartTransform();
            }
        }
    }
}