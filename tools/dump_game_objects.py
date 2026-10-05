"""Dump the object hierarchy around given script classes from the installed game data."""
import sys, os, glob
import UnityPy

DATA = r"F:\SteamLibrary\steamapps\common\Lethal Company\Lethal Company_Data"
WANT = set(sys.argv[1].split(','))
LIMIT = int(sys.argv[2]) if len(sys.argv) > 2 else 3
FILES = sys.argv[3].split(',') if len(sys.argv) > 3 else None

def v3(v):
    return f"({v.x:.2f},{v.y:.2f},{v.z:.2f})"

def describe(go, depth, out, maxdepth=4):
    comps = []
    tr = None
    for c in go.m_Component:
        ptr = c.component if hasattr(c, 'component') else c[1]
        t = ptr.type.name
        try:
            obj = ptr.deref().read(check_read=False) if t == 'MonoBehaviour' else ptr.read()
        except Exception:
            continue
        if t in ('Transform', 'RectTransform'):
            tr = obj
        elif t == 'BoxCollider':
            comps.append(f"BoxCollider(center={v3(obj.m_Center)} size={v3(obj.m_Size)} trigger={obj.m_IsTrigger} on={obj.m_Enabled})")
        elif t == 'MeshFilter':
            try:
                mesh = obj.m_Mesh.read()
                aabb = mesh.m_LocalAABB
                comps.append(f"Mesh[{mesh.m_Name}](center={v3(aabb.m_Center)} extent={v3(aabb.m_Extent)})")
            except Exception:
                comps.append("Mesh[?]")
        elif t in ('MeshRenderer', 'SkinnedMeshRenderer'):
            comps.append(f"{t}(on={obj.m_Enabled})")
        elif t == 'MonoBehaviour':
            try:
                comps.append("Script:" + obj.m_Script.read().m_ClassName)
            except Exception:
                comps.append("Script:?")
        elif t in ('NetworkObject',):
            comps.append(t)
        else:
            comps.append(t)
    pos = v3(tr.m_LocalPosition) if tr else "?"
    scl = v3(tr.m_LocalScale) if tr else "?"
    rot = tr.m_LocalRotation if tr else None
    rots = f"({rot.x:.2f},{rot.y:.2f},{rot.z:.2f},{rot.w:.2f})" if rot else "?"
    out.append(f"{'  ' * depth}{go.m_Name} active={go.m_IsActive} pos={pos} rot={rots} scale={scl} :: {' | '.join(comps)}")
    if tr and depth < maxdepth:
        for ch in tr.m_Children:
            try:
                describe(ch.read().m_GameObject.read(), depth + 1, out, maxdepth)
            except Exception as e:
                out.append(f"{'  ' * (depth + 1)}<err {e}>")

found = {}
paths = sorted(glob.glob(os.path.join(DATA, "level*")) + glob.glob(os.path.join(DATA, "sharedassets*.assets")))
paths = [p for p in paths if not p.endswith(('.resS', '.resource'))]
if FILES:
    paths = [p for p in paths if os.path.basename(p) in FILES]
for path in paths:
    try:
        env = UnityPy.load(path)
    except Exception as e:
        continue
    for obj in env.objects:
        if obj.type.name != 'MonoBehaviour':
            continue
        try:
            mb = obj.read(check_read=False)
            cls = mb.m_Script.read().m_ClassName
        except Exception:
            continue
        if cls not in WANT:
            continue
        n = found.get((cls, os.path.basename(path)), 0)
        if n >= LIMIT:
            continue
        found[(cls, os.path.basename(path))] = n + 1
        go = mb.m_GameObject.read()
        tr = None
        for c in go.m_Component:
            ptr = c.component if hasattr(c, 'component') else c[1]
            if ptr.type.name == 'Transform':
                tr = ptr.read()
        out = [f"=== {cls} in {os.path.basename(path)} on '{go.m_Name}'"]
        describe(go, 1, out, maxdepth=4)
        if tr is not None and tr.m_Father.path_id != 0:
            father = tr.m_Father.read()
            out.append(f"  -- parent '{father.m_GameObject.read().m_Name}', siblings within 8 m:")
            me = tr.m_LocalPosition
            for ch in father.m_Children:
                try:
                    st = ch.read()
                    if st.object_reader.path_id == tr.object_reader.path_id:
                        continue
                    d = st.m_LocalPosition
                    dist = ((d.x-me.x)**2 + (d.y-me.y)**2 + (d.z-me.z)**2) ** 0.5
                    if dist < 8:
                        sub = []
                        describe(st.m_GameObject.read(), 2, sub, maxdepth=3)
                        out.append(f"    [{dist:.1f} m]")
                        out.extend(sub[:6])
                except Exception as e:
                    pass
        print(chr(10).join(out[:45]))
