using Platformer.Core;
using Platformer.Mechanics;
using Platformer.Model;
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

        // Qué tan fuerte es el salto extra. El salto normal es 13.5.
        [System.NonSerialized] public float fuerzaSaltoExtra = 12.5f;   // >>> CAMBIA ESTO <<<

        // Cuánto salta si sueltas la tecla de inmediato, comparado con dejarla
        // apretada hasta el final.
        //   1    = la tecla no importa: el salto siempre sale completo
        //   0.65 = soltar de una te da un tercio de la altura (así está)
        //   0.3  = soltar te deja casi a ras del suelo
        [System.NonSerialized] public float saltoMinimo = 0.65f;      // >>> CAMBIA ESTO <<<

        PlayerController jugador;
        Animator animacion;
        InputAction accionSaltar;
        int saltosUsados;

        // Trayectoria mínima garantizada del salto normal. Va bajando con la
        // gravedad, igual que la velocidad real del personaje.
        float pisoDelSalto;
        bool saltoNormalEnCurso;
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

            // El template recorta el salto por su cuenta: al soltar la tecla multiplica
            // la velocidad por "jumpDeceleration", que vale 0, o sea que BORRA el salto
            // de golpe. Por eso se sentía de todo o nada. Lo desactivamos (1 = no
            // recorta) y a partir de aquí el único que decide es saltoMinimo.
            Simulation.GetModel<PlatformerModel>().jumpDeceleration = 1f;
        }

        // Se ejecuta después de que el jugador ya calculó su movimiento normal,
        // para que lo que escribamos aquí no se pierda.
        void LateUpdate()
        {
            if (accionSaltar == null) return;

            // El salto normal despega en el mismo frame en el que todavía estamos
            // tocando el suelo, así que hay que mirar el cambio de estado, no IsGrounded.
            var estado = jugador.jumpState;
            var acabaDeDespegar = estado == PlayerController.JumpState.Jumping
                                  && estadoPrevio != PlayerController.JumpState.Jumping;
            estadoPrevio = estado;

            if (jugador.IsGrounded && jugador.velocity.y <= 0f)
                saltosUsados = 0;

            if (jugador.controlEnabled)
            {
                if (acabaDeDespegar && jugador.velocity.y > 0f)
                {
                    pisoDelSalto = jugador.velocity.y * saltoMinimo;
                    saltoNormalEnCurso = true;
                }
                // Si estoy en el aire, presiono saltar y todavía me quedan saltos...
                else if (!jugador.IsGrounded
                         && accionSaltar.WasPressedThisFrame()
                         && saltosUsados < saltosExtra)
                {
                    saltosUsados = saltosUsados + 1;
                    jugador.velocity.y = fuerzaSaltoExtra;

                    // Los saltos extra salen enteros: la tecla ya no los recorta.
                    saltoNormalEnCurso = false;

                    if (animacion != null && animacion.HasState(0, EstadoSalto))
                        animacion.Play(EstadoSalto, 0, 0f);
                }
            }

            RecortarSaltoSiSueltas();
        }

        void RecortarSaltoSiSueltas()
        {
            // Ya dejamos de subir: llegamos al tope o chocamos con un techo.
            if (jugador.velocity.y <= 0f)
            {
                saltoNormalEnCurso = false;
                return;
            }

            if (!saltoNormalEnCurso) return;

            // El piso baja al mismo ritmo al que la gravedad frena al personaje, así
            // que nunca regala altura: solo pone un mínimo.
            pisoDelSalto = pisoDelSalto + Physics2D.gravity.y * Time.deltaTime;

            if (!accionSaltar.IsPressed() && jugador.velocity.y > pisoDelSalto)
                jugador.velocity.y = Mathf.Max(pisoDelSalto, 0f);
        }
    }
}
