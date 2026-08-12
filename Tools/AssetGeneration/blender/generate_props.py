"""
Procedurally builds the four Milestone 8 secret-room props (Bookcase_Disguise,
Desk_Office, FilingCabinet, Binder_PersonnelLogs) at their specified real-world
dimensions and exports each to its own FBX file.

Run headlessly via:
    blender --background --python generate_props.py

Or via ./generate_assets.sh, which checks for Blender and runs this for you.

Dimensions and specs match docs/ASSET_REQUESTS.md. All measurements are in
meters (Blender's default unit already equals 1 Unity unit).
"""

import bpy
import os
import random


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block_collection in (bpy.data.meshes, bpy.data.materials):
        for block in list(block_collection):
            if block.users == 0:
                block_collection.remove(block)


def make_material(name, color, roughness=0.6):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
        bsdf.inputs["Roughness"].default_value = roughness
    return mat


def uv_smart_project(obj):
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project()
    bpy.ops.object.mode_set(mode='OBJECT')


def add_box(name, size, center, material=None):
    """size and center are (x, y, z) tuples in meters."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size[0], size[1], size[2])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material is not None:
        obj.data.materials.append(material)
    uv_smart_project(obj)
    return obj


def finalize_object(pieces, final_name):
    """Joins all pieces, sets the origin to world (0,0,0) — which every
    builder below arranges to be the bottom-center of the prop — and bakes
    the resulting transform so the exported FBX has no residual offset."""
    bpy.ops.object.select_all(action='DESELECT')
    for piece in pieces:
        piece.select_set(True)
    bpy.context.view_layer.objects.active = pieces[-1]
    if len(pieces) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = final_name
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return obj


def export_fbx(obj, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True,
        object_types={'MESH'},
    )


def build_bookcase():
    """2.0m wide x 2.2m tall x 0.4m deep. Origin at bottom-center."""
    width, height, depth = 2.0, 2.2, 0.4
    side_thick = 0.03
    cap_thick = 0.03
    back_thick = 0.02
    shelf_thick = 0.025

    wood_mat = make_material("Bookcase_Wood", (0.35, 0.22, 0.12), 0.7)
    book_colors = [
        (0.6, 0.15, 0.15), (0.15, 0.3, 0.55), (0.5, 0.45, 0.1),
        (0.2, 0.4, 0.25), (0.4, 0.2, 0.45), (0.55, 0.55, 0.5),
    ]
    book_mats = [make_material(f"Book_Color_{i}", c, 0.5) for i, c in enumerate(book_colors)]

    pieces = []
    inner_w = width - 2 * side_thick
    usable_h = height - 2 * cap_thick

    pieces.append(add_box("BC_Left", (side_thick, depth, height), (-width / 2 + side_thick / 2, 0, height / 2), wood_mat))
    pieces.append(add_box("BC_Right", (side_thick, depth, height), (width / 2 - side_thick / 2, 0, height / 2), wood_mat))
    pieces.append(add_box("BC_Bottom", (inner_w, depth, cap_thick), (0, 0, cap_thick / 2), wood_mat))
    pieces.append(add_box("BC_Top", (width, depth, cap_thick), (0, 0, height - cap_thick / 2), wood_mat))
    pieces.append(add_box("BC_Back", (inner_w, back_thick, usable_h), (0, depth / 2 - back_thick / 2, height / 2), wood_mat))

    shelf_count = 5
    shelf_zs = []
    for i in range(shelf_count):
        z = cap_thick + (i + 1) * (usable_h / (shelf_count + 1))
        shelf_zs.append(z)
        pieces.append(add_box(
            f"BC_Shelf_{i}",
            (inner_w, depth - back_thick - 0.02, shelf_thick),
            (0, -back_thick / 2 - 0.01, z),
            wood_mat,
        ))

    rng = random.Random(1234)
    book_index = 0
    for shelf_i in (0, 2, 3):
        z = shelf_zs[shelf_i] + shelf_thick / 2
        x_cursor = -inner_w / 2 + 0.05
        books_on_shelf = rng.randint(2, 3)
        for _ in range(books_on_shelf):
            book_w = rng.uniform(0.05, 0.09)
            book_h = rng.uniform(0.18, 0.26)
            book_d = depth - back_thick - 0.06
            if x_cursor + book_w > inner_w / 2 - 0.05:
                break
            mat = book_mats[book_index % len(book_mats)]
            book_index += 1
            pieces.append(add_box(
                f"BC_Book_{shelf_i}_{book_index}",
                (book_w, book_d, book_h),
                (x_cursor + book_w / 2, -back_thick / 2 - 0.01, z + book_h / 2),
                mat,
            ))
            x_cursor += book_w + 0.01

    return finalize_object(pieces, "Bookcase_Disguise")


def build_desk():
    """1.4m wide x 0.75m tall x 0.7m deep. Origin at bottom-center."""
    width, depth, height = 1.4, 0.7, 0.75
    top_thick = 0.03
    leg_size = 0.05

    laminate_mat = make_material("Desk_Laminate", (0.75, 0.68, 0.55), 0.5)
    metal_mat = make_material("Desk_Metal", (0.15, 0.15, 0.15), 0.35)

    pieces = [add_box("Desk_Top", (width, depth, top_thick), (0, 0, height - top_thick / 2), laminate_mat)]

    leg_h = height - top_thick
    inset = 0.05
    for sx in (-1, 1):
        for sy in (-1, 1):
            x = sx * (width / 2 - inset)
            y = sy * (depth / 2 - inset)
            pieces.append(add_box(f"Desk_Leg_{sx}_{sy}", (leg_size, leg_size, leg_h), (x, y, leg_h / 2), metal_mat))

    drawer_w, drawer_d, drawer_h = 0.35, depth - 0.1, 0.18
    drawer_x = width / 2 - drawer_w / 2 - 0.05
    drawer_z = height - top_thick - drawer_h / 2
    pieces.append(add_box("Desk_Drawer", (drawer_w, drawer_d, drawer_h), (drawer_x, 0, drawer_z), metal_mat))

    return finalize_object(pieces, "Desk_Office")


def build_filing_cabinet():
    """0.45m wide x 1.3m tall x 0.6m deep. Origin at bottom-center."""
    width, depth, height = 0.45, 0.6, 1.3

    cabinet_mat = make_material("Cabinet_Body", (0.55, 0.55, 0.58), 0.4)
    handle_mat = make_material("Cabinet_Handle", (0.2, 0.2, 0.2), 0.3)

    pieces = [add_box("Cabinet_Body", (width, depth, height), (0, 0, height / 2), cabinet_mat)]

    drawer_count = 4
    gap = 0.015
    drawer_h = (height - gap * (drawer_count + 1)) / drawer_count
    for i in range(drawer_count):
        z = gap + i * (drawer_h + gap) + drawer_h / 2
        pieces.append(add_box(f"Cabinet_DrawerFace_{i}", (width - 0.04, 0.02, drawer_h), (0, -depth / 2 - 0.01, z), cabinet_mat))
        pieces.append(add_box(f"Cabinet_Handle_{i}", (width * 0.4, 0.03, 0.03), (0, -depth / 2 - 0.03, z), handle_mat))

    return finalize_object(pieces, "FilingCabinet")


def build_binder():
    """0.3m wide x 0.25m tall x 0.05m thick. Origin at bottom-center."""
    width, height, thickness = 0.3, 0.25, 0.05

    cover_mat = make_material("Binder_Cover", (0.08, 0.08, 0.18), 0.4)
    label_mat = make_material("Binder_Label", (0.65, 0.65, 0.65), 0.5)

    main = add_box("Binder_Main", (width, thickness, height), (0, 0, height / 2), cover_mat)

    bevel = main.modifiers.new(name="Bevel", type='BEVEL')
    bevel.width = 0.005
    bevel.segments = 2
    bpy.context.view_layer.objects.active = main
    bpy.ops.object.modifier_apply(modifier="Bevel")

    label = add_box("Binder_Label", (0.06, thickness + 0.005, height * 0.4), (-width / 2 + 0.03, 0, height * 0.6), label_mat)

    return finalize_object([main, label], "Binder_PersonnelLogs")


def main():
    output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "output")
    os.makedirs(output_dir, exist_ok=True)

    builders = [
        (build_bookcase, "Bookcase_Disguise.fbx"),
        (build_desk, "Desk_Office.fbx"),
        (build_filing_cabinet, "FilingCabinet.fbx"),
        (build_binder, "Binder_PersonnelLogs.fbx"),
    ]

    for builder, filename in builders:
        clear_scene()
        obj = builder()
        filepath = os.path.join(output_dir, filename)
        export_fbx(obj, filepath)
        print(f"[generate_props] Exported '{filename}' -> {filepath}")

    clear_scene()
    print(f"[generate_props] All assets generated successfully in: {output_dir}")


main()
