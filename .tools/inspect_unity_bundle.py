import argparse
import json

import UnityPy


def vector(value):
    names = ("x", "y", "z", "w")
    return [getattr(value, name) for name in names if hasattr(value, name)]


def component_name(pointer):
    obj = pointer.assetsfile.objects.get(pointer.path_id)
    if obj is None:
        return "Missing"
    if obj.type.name != "MonoBehaviour":
        return obj.type.name
    try:
        data = obj.read()
        script = data.m_Script.read()
        return f"MonoBehaviour:{script.m_Namespace}.{script.m_ClassName}"
    except Exception:
        return "MonoBehaviour:unknown"


def describe_game_object(game_object):
    data = game_object.read()
    return {
        "path_id": game_object.path_id,
        "name": data.m_Name,
        "active": data.m_IsActive,
        "layer": data.m_Layer,
        "tag": data.m_Tag,
        "components": [component_name(pair.component) for pair in data.m_Component],
    }


def describe_transform(transform_pointer, depth=0, max_depth=None):
    transform_obj = transform_pointer.assetsfile.objects[transform_pointer.path_id]
    transform = transform_obj.read()
    game_object = transform.m_GameObject.assetsfile.objects[transform.m_GameObject.path_id]
    result = describe_game_object(game_object)
    result.update(
        {
            "local_position": vector(transform.m_LocalPosition),
            "local_rotation": vector(transform.m_LocalRotation),
            "local_scale": vector(transform.m_LocalScale),
            "children": [],
        }
    )
    if max_depth is None or depth < max_depth:
        result["children"] = [
            describe_transform(child, depth + 1, max_depth)
            for child in transform.m_Children
        ]
    return result


def find_game_object(environment, name):
    for obj in environment.objects:
        if obj.type.name == "GameObject" and obj.read().m_Name == name:
            return obj
    raise RuntimeError(f"GameObject not found: {name}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("bundle")
    parser.add_argument("root_name")
    parser.add_argument("--max-depth", type=int)
    args = parser.parse_args()

    environment = UnityPy.load(args.bundle)
    root = find_game_object(environment, args.root_name)
    root_data = root.read()
    transform_pointer = next(
        pair.component
        for pair in root_data.m_Component
        if pair.component.assetsfile.objects[pair.component.path_id].type.name == "Transform"
    )
    print(json.dumps(describe_transform(transform_pointer, max_depth=args.max_depth), indent=2))


if __name__ == "__main__":
    main()
