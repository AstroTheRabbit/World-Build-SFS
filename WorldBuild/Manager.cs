using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using SFS;
using SFS.UI;
using SFS.Input;
using SFS.World;
using SFS.Parts;
using SFS.Cameras;
using SFS.Parts.Modules;
using static SFS.Builds.BuildGrid;
using WorldBuild.GUI;

namespace WorldBuild
{
    public class Manager : MonoBehaviour
    {
        public static Manager main;
        public bool worldBuildActive;
        public bool draggingPart;

        Part heldPart;
        Rocket closestRocket;
        Vector2 partTargetPos;
        List<Collider2D> disabledColliders = new List<Collider2D>();
        Dictionary<Mesh, List<Color32>> defaultMeshColors = new Dictionary<Mesh, List<Color32>>();
        PartPlacementState _partState;
        PartPlacementState PartPlacementState
        {
            get
            {
                return _partState;
            }
            set
            {
                _partState = value;
                SetPartColor(_partState == PartPlacementState.Allowed ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f));
            }
        }

        void Update()
        {
            if (heldPart == null)
                return;
            
            // * Update closest rocket and create rocket build colliders.
            // TODO: Optimise part clipping detection? (currently the code just rebuilds part/rocket colliders every time the part is transformed).
            float bestDist = 0f;
            int? bestIdx = null;
            Dictionary<Rocket, List<PartCollider>> rocketColliders = new Dictionary<Rocket, List<PartCollider>>();
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
                    if (dist <= 10f && !Base.worldBase.settings.cheats.partClipping)
                    {
                        rocketColliders.Add(rocket, CreateBuildColliders(rocket.partHolder.GetArray()));
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

            // * Update part placement validity.
            foreach (ConvexPolygon partPoly in CreateBuildColliders(heldPart).SelectMany((PartCollider col) => col.colliders))
            {
                foreach (KeyValuePair<Rocket, List<PartCollider>> kvp in rocketColliders)
                {
                    foreach (ConvexPolygon rocketPoly in kvp.Value.SelectMany((PartCollider col) => col.colliders))
                    {
                        if (ConvexPolygon.Intersect(partPoly, rocketPoly, -0.08f))
                        {
                            PartPlacementState = PartPlacementState.ClippingRocket;
                            return;
                        }
                    }
                }

                // ! TODO: Fix part/terrain clipping detection
                // foreach (Vector2 point in partPoly.points)
                // {
                //     Double2 worldPos = WorldView.ToGlobalPosition(heldPart.transform.TransformPoint(point));
                //     if (WorldView.main.ViewLocation.planet.IsInsideTerrain(worldPos, -15))
                //     {
                //         PartPlacementState = PartPlacementState.ClippingTerrain;
                //         return;
                //     }
                // }
            }
            PartPlacementState = PartPlacementState.Allowed;
        }

        List<PartCollider> CreateBuildColliders(params Part[] parts)
        {
            List<PartCollider> buildColliders = new List<PartCollider>();
            for (int i = 0; i < parts.Length; i++)
            {
                PolygonData[] modules = parts[i].GetModules<PolygonData>();
                foreach (PolygonData polygonData in modules)
                {
                    if (polygonData.BuildCollider /* _IncludeInactive */)
                    {
                        PartCollider partCollider = new PartCollider
                        {
                            module = polygonData,
                            colliders = null
                        };
                        partCollider.UpdateColliders();
                        buildColliders.Add(partCollider);
                    }
                }
            }
            return buildColliders;
        }

        public void EnterBuild()
        {
            worldBuildActive = true;
            PartPickerUI.CreateUI();
        }

        public void ExitBuild()
        {
            draggingPart = false;
            worldBuildActive = false;
            PartPickerUI.DestroyUI();
            DestroyHeldPart();
        }

        public void ToggleBuild()
        {
            if (PartPickerUI.GUIHolder == null)
            {
                EnterBuild();
            } else {
                ExitBuild();
            }
        }

        public void CreateNewPart(VariantRef variant, Vector2 mousePos)
        {
            DestroyHeldPart();
            
            heldPart = PartsLoader.CreatePart(variant, true);
            heldPart.transform.parent = transform;
            heldPart.transform.position = mousePos;
            partTargetPos = mousePos;
            draggingPart = true;

            foreach (Collider2D col in heldPart.GetModules<Collider2D>())
            {
                if (col.isActiveAndEnabled && !col.isTrigger)
                {
                    disabledColliders.Add(col);
                    col.enabled = false;
                }
            }

            Update();
        }

        void DestroyHeldPart()
        {
            disabledColliders.Clear();
            try { heldPart.DestroyPart(false, false, DestructionReason.Intentional); } catch (NullReferenceException) { }
        }

        public void TryBuildPart()
        {
            if (heldPart == null)
                return;

            if (PartPlacementState == PartPlacementState.ClippingRocket)
            {
                MsgDrawer.main.Log("Cannot build part inside another part!");
                return;
            }
            else if (PartPlacementState == PartPlacementState.ClippingTerrain)
            {
                MsgDrawer.main.Log("Cannot build part inside the ground!");
                return;
            }

            foreach (Collider2D col in disabledColliders)
            {
                col.enabled = true;
            }

            if (closestRocket != null)
            {
                heldPart.transform.parent = closestRocket.partHolder.transform;
                Part[] parts = closestRocket.partHolder.GetArray().AddItem(heldPart).ToArray();
                new JointGroup(RocketManager.GenerateJoints(parts), parts.ToList()).RecreateGroups(out List<JointGroup> jointGroups);
                if (jointGroups.Count == 1)
                {
                    // * Part was attached to rocket.
                    closestRocket.SetJointGroup(jointGroups[0]);
                    goto Reset;
                }
            }
            // * Part was NOT attached to rocket.
            JointGroup group = new JointGroup(new List<PartJoint>(), new List<Part>() { heldPart });
            Rocket rocket = Instantiate(AccessTools.StaticFieldRefAccess<RocketManager, Rocket>("prefab"));
            
            // rocket.rocketName = "";
            Debug.Log($"INIT: {rocket.location.Value != null}");
            rocket.physics.SetLocationAndState
            (
                new Location
                (
                    WorldTime.main.worldTime,
                    WorldView.main.ViewLocation.planet,
                    WorldView.ToGlobalPosition(heldPart.transform.position),
                    Double2.zero
                ),
                false
            );
            Debug.Log($"PHYS: {rocket.location.Value != null}");
            rocket.stats.Load(-1);
            Debug.Log($"STAT: {rocket.location.Value != null}");
            rocket.SetJointGroup(group);
            heldPart.transform.localPosition = Vector3.zero;

            Reset:
                ResetPartColor();
                draggingPart = false;
                heldPart = null;
        }

        public void SetPartColor(Color color)
        {
            if (heldPart != null)
            {
                foreach (BaseMesh partMesh in heldPart.GetModules<BaseMesh>())
                {
                    Mesh mesh = AccessTools.FieldRefAccess<BaseMesh, Mesh>("meshReference").Invoke(partMesh);
                    if (!defaultMeshColors.ContainsKey(mesh))
                    {
                        List<Color32> colors = new List<Color32>();
                        mesh.GetColors(colors);
                        defaultMeshColors.Add(mesh, colors);
                    }
                    mesh.SetColors(Enumerable.Repeat(color, mesh.vertices.Length).ToList());
                }
            }
        }

        public void ResetPartColor()
        {
            // ! TODO: For some stupid reason this causes a null ref in `Mesh.SetSizedArrayForChannel(Impl)`?!
            // foreach (KeyValuePair<Mesh, List<Color32>> kvp in defaultMeshColors)
            // {
            //     kvp.Key.SetColors(kvp.Value);
            // }
            defaultMeshColors.Clear();
        }

        public void AddInputs()
        {
            Screen_Game input = GameManager.main.world_Input;
            input.onInputStart += OnInputStart;
            input.onInputEnd += OnInputEnd;
            input.onDrag += OnDrag;
            ActiveCamera.Camera.position.OnChange += OnCameraPositionChange;

            void OnInputStart(OnInputStartData data)
            {
                if (data.inputType == InputType.MouseLeft && heldPart != null)
                {
                    Vector2 pos = data.position.World(0f);
                    draggingPart = Part_Utility.RaycastParts(new [] { heldPart }, pos, 0.3f, out PartHit _);
                }
            }

            void OnInputEnd(OnInputEndData data)
            {
                if (data.LeftClick && heldPart != null)
                {
                    draggingPart = false;
                }
            }

            void OnDrag(DragData data)
            {
                if (draggingPart && heldPart != null)
                {
                    partTargetPos -= data.DeltaWorld(0f);
                }
            }

            void OnCameraPositionChange(Vector2 oldPos, Vector2 newPos)
            {
                if (heldPart != null)
                {
                    partTargetPos += newPos - oldPos;
                }
            }
        }
    }

    enum PartPlacementState
    {
        ClippingTerrain,
        ClippingRocket,
        Allowed,
    }
}