// SHAIQAS Core · Соққылар (MOVES) және ойыншының ниеті (Intent)
using UnityEngine;
public class Intent
{
    public int move, dash;
    public bool up, down, j, k, l, u;   // u — балалар режимінде бір батырмамен әдіс
    public int sb;                       // әдіс тақтасы: 1..4 (0 — басылмады)
    public void Clear() { move = 0; dash = 0; up = false; down = false; j = false; k = false; l = false; u = false; sb = 0; }
}

public class Move
{
    public string id = "", pw = "guard", ps = "guard", alt = "", height = "mid";
    public float w, a, r, dmg, reach, knock, hitstop, lunge, leap;
    public int hits = 1;
    public bool heavy, down, foot, low, air, spin, after, special, wave, anti, thr, sk, unblock;

    public static Move Base(string id)
    {
        JNode m = Db.Moves.Get(id);
        Move o = new Move();
        o.id = id;
        if (m.IsNull()) { o.w = 0.1f; o.a = 0.1f; o.r = 0.2f; o.dmg = 5; o.reach = 200; o.knock = 150; o.pw = "lightW"; o.ps = "lightS"; return o; }
        o.w = m.Fk("w"); o.a = m.Fk("a"); o.r = m.Fk("r"); o.dmg = m.Fk("dmg"); o.reach = m.Fk("reach"); o.knock = m.Fk("knock");
        o.hitstop = m.Fk("hitstop"); o.lunge = m.Fk("lunge"); o.leap = m.Fk("leap"); o.hits = m.Get("hits").I(1);
        o.pw = m.S("pw"); o.ps = m.S("ps"); o.alt = m.Get("alt").Str(""); o.height = m.S("height");
        o.heavy = m.Bk("heavy"); o.down = m.Bk("down"); o.foot = m.Bk("foot"); o.low = m.Bk("low"); o.air = m.Bk("air");
        o.spin = m.Bk("spin"); o.after = m.Bk("after"); o.special = m.Bk("special"); o.wave = m.Bk("wave"); o.anti = m.Bk("anti");
        o.thr = m.Bk("throw");
        return o;
    }
    public Move Copy()
    {
        Move o = new Move();
        o.id = id; o.pw = pw; o.ps = ps; o.alt = alt; o.height = height;
        o.w = w; o.a = a; o.r = r; o.dmg = dmg; o.reach = reach; o.knock = knock; o.hitstop = hitstop; o.lunge = lunge; o.leap = leap; o.hits = hits;
        o.heavy = heavy; o.down = down; o.foot = foot; o.low = low; o.air = air; o.spin = spin; o.after = after; o.special = special; o.wave = wave; o.anti = anti; o.thr = thr; o.sk = sk; o.unblock = unblock;
        return o;
    }
    public bool IsHigh() { return U.Eq(height, "high"); }
    public bool IsLow() { return U.Eq(height, "low"); }
}
