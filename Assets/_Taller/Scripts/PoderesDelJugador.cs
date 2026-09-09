using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Taller
{
    // ============================================================
    //  PODERES DEL JUGADOR
    //
    //  Aquí viven los superpoderes. Con una sola variable puedes
    //  darle doble salto (o triple, o ninguno) a tu personaje.
    // ============================================================
    [RequireComponent(typeof(PlayerController))]
    public class PoderesDelJugador : MonoBehaviour
    {
        // Estas dos variables se cambian SOLO aquí, en el código.
        // (Por eso llevan [NonSerialized]: así Unity no guarda una copia en la
        //  escena que le gane a lo que tú escribas.)

        // 0 = salto normal.  1 = doble salto.  2 = triple salto.
        [System.NonSerialized] public int saltosExtra = 1;           // >>> CAMBIA ESTO <<<

        // Qué tan fuerte es el salto extra. El salto normal es 7.
        [System.NonSerialized] public float fuerzaSaltoExtra = 7f;   // >>> CAMBIA ESTO <<<

        PlayerController jugador;
        Animator animacion;
        InputAction accionSaltar;
        int saltosUsados;

        // El Animator solo sabe si estas en el suelo o no, asi que en el salto extra
        // hay que pedirle explicitamente que vuelva a reproducir el salto desde el
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

            // Si estoy pisando el suelo, se me recargan los saltos extra.
            if (jugador.IsGrounded)
            {
                saltosUsados = 0;
                return;
            }

            if (!jugador.controlEnabled) return;

            // Si estoy en el aire, presiono saltar y todavía me quedan saltos...
            if (accionSaltar.WasPressedThisFrame() && saltosUsados < saltosExtra)
            {
                saltosUsados = saltosUsados + 1;
                jugador.velocity.y = fuerzaSaltoExtra;

                if (animacion != null && animacion.HasState(0, EstadoSalto))
                    animacion.Play(EstadoSalto, 0, 0f);
            }
        }
    }
}
