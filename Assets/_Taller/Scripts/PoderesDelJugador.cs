using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Taller
{
    // ============================================================
    //  PODERES DEL JUGADOR
    //
    //  Aquí viven los superpoderes y la sensación del salto.
    //  Con una sola variable puedes darle doble salto (o triple,
    //  o ninguno) a tu personaje.
    // ============================================================
    [RequireComponent(typeof(PlayerController))]
    public class PoderesDelJugador : MonoBehaviour
    {
        // Estas variables se cambian SOLO aquí, en el código.
        // (Por eso llevan [NonSerialized]: así Unity no guarda una copia en la
        //  escena que le gane a lo que tú escribas.)

        // 0 = salto normal.  1 = doble salto.  2 = triple salto.
        [System.NonSerialized] public int saltosExtra = 1;           // >>> CAMBIA ESTO <<<

        // Qué tan fuerte es el salto extra. El salto normal es 7.
        [System.NonSerialized] public float fuerzaSaltoExtra = 7f;   // >>> CAMBIA ESTO <<<

        // Cuánto salta si solo TOCAS la tecla, sin dejarla apretada.
        //   1    = la tecla no importa, siempre salta completo
        //   0.7  = tocar da la mitad de la altura (así está ahora)
        //   0    = tocar apenas casi no te levanta
        [System.NonSerialized] public float saltoMinimo = 0.7f;      // >>> CAMBIA ESTO <<<

        PlayerController jugador;
        Animator animacion;
        InputAction accionSaltar;
        int saltosUsados;

        // Mientras un salto sube, guardamos aquí la velocidad que le corresponde y no
        // dejamos que baje de ahí. El valor decae al mismo ritmo que la gravedad, así
        // que nunca regala altura: solo impide que soltar la tecla lo corte en seco.
        float pisoDelSalto;
        bool protegiendoSalto;
        PlayerController.JumpState estadoPrevio;

        // El Animator solo sabe si estás en el suelo o no, así que en el salto extra
        // hay que pedirle explícitamente que vuelva a reproducir el salto desde el
        // principio. Si no, el personaje parece flotar.
        static readonly int EstadoSalto = Animator.StringToHash("Player-Jump");

        void Awake()
        {
            jugador = GetComponent<PlayerController>();
            animacion = GetComponent<Animator>();
            accionSaltar = InputSystem.actions.FindAction("Player/Jump");
        }

        // Se ejecuta después de que el jugador ya calculó su movimiento normal,
        // para que el salto extra no se pierda.
        void LateUpdate()
        {
            if (accionSaltar == null) return;

            // El salto normal despega en el mismo frame en el que todavía estamos
            // tocando el suelo, así que hay que mirar el cambio de estado, no IsGrounded.
            var estado = jugador.jumpState;
            var acabaDeDespegar = estado == PlayerController.JumpState.Jumping
                                  && estadoPrevio != PlayerController.JumpState.Jumping;
            estadoPrevio = estado;

            if (jugador.IsGrounded)
            {
                saltosUsados = 0;
                if (!acabaDeDespegar) protegiendoSalto = false;
            }

            if (jugador.controlEnabled)
            {
                if (acabaDeDespegar && jugador.velocity.y > 0f)
                {
                    // Salto normal: garantizamos una altura mínima aunque sueltes al instante.
                    ProtegerSalto(jugador.velocity.y * saltoMinimo);
                }
                // Si estoy en el aire, presiono saltar y todavía me quedan saltos...
                else if (!jugador.IsGrounded
                         && accionSaltar.WasPressedThisFrame()
                         && saltosUsados < saltosExtra)
                {
                    saltosUsados = saltosUsados + 1;
                    jugador.velocity.y = fuerzaSaltoExtra;

                    // El salto extra se protege entero: no depende de cuánto tiempo
                    // dejes apretada la tecla.
                    ProtegerSalto(fuerzaSaltoExtra);

                    if (animacion != null && animacion.HasState(0, EstadoSalto))
                        animacion.Play(EstadoSalto, 0, 0f);
                }
            }

            SostenerSalto();
        }

        void ProtegerSalto(float velocidad)
        {
            pisoDelSalto = velocidad;
            protegiendoSalto = true;
        }

        void SostenerSalto()
        {
            if (!protegiendoSalto) return;

            // El piso baja al mismo ritmo al que la gravedad frena al personaje.
            pisoDelSalto = pisoDelSalto + Physics2D.gravity.y * Time.deltaTime;

            // Llegamos al tope del salto, o chocamos con un techo: ya no hay nada
            // que proteger.
            if (pisoDelSalto <= 0f || jugador.velocity.y <= 0f)
            {
                protegiendoSalto = false;
                return;
            }

            if (jugador.velocity.y < pisoDelSalto)
                jugador.velocity.y = pisoDelSalto;
        }
    }
}
