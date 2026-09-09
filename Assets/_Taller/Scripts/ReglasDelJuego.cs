using System.Collections;
using Platformer.Core;
using Platformer.Gameplay;
using Platformer.Mechanics;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Taller
{
    // ============================================================
    //  REGLAS DEL JUEGO  -  este es el archivo del taller.
    //
    //  Aquí se decide: cuántas monedas hay que juntar, cuántas
    //  vidas tienes y cuántos puntos vale cada moneda.
    //
    //  Busca los comentarios que dicen  >>> CAMBIA ESTO <<<
    // ============================================================
    public class ReglasDelJuego : MonoBehaviour
    {
        // ---------- 1. VARIABLES ----------
        // Una variable es una cajita con un nombre que guarda un valor.
        // Cambia los números, guarda con Ctrl+S y vuelve a darle Play.
        //
        // Estas variables se cambian SOLO aquí, en el código. Por eso llevan
        // [NonSerialized]: para que Unity no guarde una copia dentro de la
        // escena que le gane a lo que tú escribas.

        // Cuántas monedas hay que juntar para que se abra la meta.
        [System.NonSerialized] public int monedasParaGanar = 5;      // >>> CAMBIA ESTO <<<

        // Cuántas veces puedes morir antes de perder.
        [System.NonSerialized] public int vidas = 3;                 // >>> CAMBIA ESTO <<<

        // Puntos que suma cada moneda.
        [System.NonSerialized] public int puntosPorMoneda = 10;      // >>> CAMBIA ESTO <<<

        // Puntos extra por cada moneda que recojas de más.
        [System.NonSerialized] public int bonoPorMonedaExtra = 50;   // >>> CAMBIA ESTO <<<

        // Cuánto dura la celebración antes de que salga el cartel de GANASTE.
        [System.NonSerialized] public float segundosDeCelebracion = 1.8f;   // >>> CAMBIA ESTO <<<

        [Header("Conexiones (esto ya viene puesto, no hay que tocarlo)")]
        public VictoryZone meta;
        public TMP_Text textoMonedas;
        public TMP_Text textoVidas;
        public TMP_Text textoPuntos;
        public TMP_Text textoMensaje;
        public GameObject cartelMensaje;
        public GameObject panelVictoria;
        public GameObject panelDerrota;
        public TMP_Text resumenVictoria;
        public TMP_Text resumenDerrota;

        // Estas variables las lleva el juego solo mientras juegas.
        int monedasRecogidas;
        int puntos;
        int vidasRestantes;
        bool juegoTerminado;
        bool puedeReiniciar;
        bool muerteYaContada;
        Collider2D colliderDeLaMeta;

        // Colores del marcador de monedas: gris mientras faltan, dorado al completarlas.
        static readonly Color Apagado = new Color(0.94f, 0.97f, 1f);
        static readonly Color Dorado = new Color(0.98f, 0.80f, 0.24f);

        // Lo que tarda el cartel de PERDISTE. Tiene que ser menor que los 2 segundos
        // que el juego espera para revivirte, o reaparecerías detrás del cartel.
        const float SegundosDeDerrota = 1.2f;

        // ---------- 2. LO QUE PASA CUANDO RECOGES UNA MONEDA ----------
        void AlRecogerMoneda(PlayerTokenCollision evento)
        {
            monedasRecogidas = monedasRecogidas + 1;
            puntos = puntos + puntosPorMoneda;

            // Un "if" es una pregunta: si la respuesta es sí, hace lo de adentro.
            // >>> CAMBIA ESTO <<<  prueba con  <  o con  ==  y mira qué cambia.
            if (monedasRecogidas > monedasParaGanar)
            {
                puntos = puntos + bonoPorMonedaExtra;
            }

            ActualizarPantalla();
        }

        // ---------- 3. LA REGLA QUE ABRE LA META ----------
        void ActualizarPantalla()
        {
            if (textoMonedas != null)
                textoMonedas.text = monedasRecogidas + " / " + monedasParaGanar;

            if (textoVidas != null)
                textoVidas.text = "× " + vidasRestantes;

            if (textoPuntos != null)
                textoPuntos.text = puntos + " PTS";

            // >>> CAMBIA ESTO <<<
            // Mientras no tengas suficientes monedas, la meta está cerrada.
            if (monedasRecogidas >= monedasParaGanar)
            {
                AbrirLaMeta(true);
                MostrarMensaje("¡La meta está abierta! Corre hacia el arco");
            }
            else
            {
                AbrirLaMeta(false);
                int faltan = monedasParaGanar - monedasRecogidas;
                MostrarMensaje("Te faltan " + faltan + " monedas");
            }
        }

        // ---------- 4. LO QUE PASA CUANDO TE MATAN ----------
        void AlMorir(PlayerDeath evento)
        {
            if (juegoTerminado) return;
            if (muerteYaContada) return;   // evita restar dos vidas por una sola caída
            muerteYaContada = true;

            vidasRestantes = vidasRestantes - 1;
            ActualizarPantalla();

            if (vidasRestantes <= 0)
            {
                juegoTerminado = true;
                StartCoroutine(TerminarDentroDeUnRato(panelDerrota, resumenDerrota, SegundosDeDerrota));
            }
        }

        void AlReaparecer(PlayerSpawn evento)
        {
            muerteYaContada = false;
        }

        // ---------- 5. LO QUE PASA CUANDO LLEGAS A LA META ----------
        void AlLlegarALaMeta(PlayerEnteredVictoryZone evento)
        {
            if (juegoTerminado) return;
            juegoTerminado = true;
            StartCoroutine(TerminarDentroDeUnRato(panelVictoria, resumenVictoria, segundosDeCelebracion));
        }

        // ============================================================
        //  De aquí para abajo son las tuercas y tornillos.
        //  No hace falta entenderlo todo para el taller.
        // ============================================================

        void AbrirLaMeta(bool abierta)
        {
            if (colliderDeLaMeta != null)
                colliderDeLaMeta.enabled = abierta;

            if (textoMonedas != null)
                textoMonedas.color = abierta ? Dorado : Apagado;
        }

        void MostrarMensaje(string texto)
        {
            if (textoMensaje != null) textoMensaje.text = texto;
            if (cartelMensaje != null) cartelMensaje.SetActive(texto != "");
        }

        // Esperamos un momento antes de tapar la pantalla, para que se vea la
        // animación de celebración o de muerte del personaje. Si congelamos el
        // juego de una, el Animator tambien se congela y no se ve nada.
        IEnumerator TerminarDentroDeUnRato(GameObject panel, TMP_Text resumen, float segundos)
        {
            yield return new WaitForSeconds(segundos);

            MostrarMensaje("");

            // >>> CAMBIA ESTO <<<  lo que aparece en el cartel del final.
            if (resumen != null)
                resumen.text = "Monedas " + monedasRecogidas + " / " + monedasParaGanar
                             + "     Puntos " + puntos;

            if (panel != null) panel.SetActive(true);
            puedeReiniciar = true;
            Time.timeScale = 0f;
        }

        void Start()
        {
            Time.timeScale = 1f;
            monedasRecogidas = 0;
            puntos = 0;
            vidasRestantes = vidas;
            juegoTerminado = false;
            puedeReiniciar = false;
            muerteYaContada = false;

            if (meta != null) colliderDeLaMeta = meta.GetComponent<Collider2D>();
            if (panelVictoria != null) panelVictoria.SetActive(false);
            if (panelDerrota != null) panelDerrota.SetActive(false);

            ActualizarPantalla();
        }

        void Update()
        {
            if (!puedeReiniciar) return;

            var teclado = Keyboard.current;
            if (teclado != null && teclado.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                Simulation.Clear();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        void OnEnable()
        {
            PlayerTokenCollision.OnExecute += AlRecogerMoneda;
            PlayerDeath.OnExecute += AlMorir;
            PlayerSpawn.OnExecute += AlReaparecer;
            PlayerEnteredVictoryZone.OnExecute += AlLlegarALaMeta;
        }

        void OnDisable()
        {
            PlayerTokenCollision.OnExecute -= AlRecogerMoneda;
            PlayerDeath.OnExecute -= AlMorir;
            PlayerSpawn.OnExecute -= AlReaparecer;
            PlayerEnteredVictoryZone.OnExecute -= AlLlegarALaMeta;
        }
    }
}
