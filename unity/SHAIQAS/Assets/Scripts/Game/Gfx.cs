// SHAIQAS · Графика сапасы: компьютерде — толық, телефонда — жеңілдетілген (ойын 60 кадрда жүрсін).
using UnityEngine;

public static class Gfx
{
    public static bool Mobile;
    public static int OutlineDirs = 8;      // контур өтулерінің саны (әр өту — жауынгерді бір рет салу)
    public static float OutlineW = 2.4f;    // контур қалыңдығы (әлем пикселі)
    public static float Shade = 0.17f;      // жарық/көлеңке жақтың айырмасы
    public static float BlurFar = 2.2f, BlurMid = 1.2f;   // алыс фон қабаттарының бұлдырлығы (камера фокусы)
    public static bool Shock = true;        // соққы толқынының ауаны қисайтуы, нокаутта түс ығысуы, Хиуадағы ыстық сағым

    public static void Init()
    {
        Mobile = Application.isMobilePlatform;
        if (Mobile)
        {
            OutlineDirs = 4; OutlineW = 2.6f; BlurFar = 0; BlurMid = 0;
            QualitySettings.antiAliasing = 2;
        }
        else QualitySettings.antiAliasing = 4;   // жауынгерлер мен әсерлердің жиегіндегі «баспалдақтар» жоғалады
    }
}
