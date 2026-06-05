# Generated Mesh Pipeline

## Purpose

Register externally generated meshes and prefabs in the deterministic asset library without adding runtime AI generation or runtime GLB loading.

## Recommended 2D vs 3D split

- Keep as billboards:
  - NPCs and humanoids
  - loot and items
  - foliage clumps
  - small decorative props
- Keep as true 3D:
  - buildings
  - mine entrances
  - wells, bridges, doors, stairs
  - terrain chunks
  - collision-critical structures

## Workflow

1. Generate or clean the mesh outside Unity.
2. Import the resulting FBX/GLB/prefab into Unity by the normal asset pipeline.
3. Open `Ember > Generated Assets > Library`.
4. Select a mesh-kind record and assign the imported prefab/model to `Mesh Source`.
5. Click `Analyze Source` to capture triangles, vertices, materials, texture paths, collider presence, and LOD presence.
6. Optionally `Build Mesh Job` / `Dry Run Mesh Tool` to export deterministic metadata for an external mesh tool.
7. Click `Create Prefab` to save a generated-library prefab under `Assets/Art/Prefabs/Generated`.
8. Validate the mesh against triangle budgets and review gates.

## Limits

- No runtime external process calls.
- No runtime mesh generation.
- No automatic retopology, UV unwrap, rigging, or mesh cleanup.
- If glTF import packages are absent, register already-imported Unity assets instead of adding dependency churn.

## Runtime policy

- Runtime resolves approved mesh/prefab records only.
- Save data should keep stable ids or key fields, not ad-hoc asset paths.
