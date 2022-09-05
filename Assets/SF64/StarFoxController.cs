using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class StarFoxController : MonoBehaviour
{
    [FormerlySerializedAs("cameraTarget")] [SerializeField] private Transform moveTarget;
    [SerializeField] private Transform shipTarget;
    [SerializeField] private Transform reticleTarget;
    [SerializeField] private Vector2 minMaxElevation;
    [SerializeField] private Vector2 minMaxStrafe;

    [SerializeField] private Vector2 rangeOfShipMovement = new(3f, 2f);
    [SerializeField] private Vector2 rangeOfReticleMovement = new(50f, 25f);

    [FormerlySerializedAs("reticleSharpness")] [SerializeField]
    private float reticleResponsiveness = 8f;

    [SerializeField] private Vector2 shipMoveSpeed = new Vector2(3f, 1f);
    [SerializeField] private float reticleDistance = 50f;
    [SerializeField] private float shipDistance = 7f;

    [SerializeField] private Vector2 deadZone = new(.2f, .2f);

    [FormerlySerializedAs("camSpeed")]
    [SerializeField] private Vector3 worldSpeed = new(30f, 10f, 50f);

    [SerializeField] private float camMaxYaw = 30f;
    [SerializeField] private float camMaxRoll = 10f;
    [SerializeField] private float camMaxPitch = 10f;
    [SerializeField] private float cameraRotationSharpness;

    private Vector2 _input;
    private Vector2 _inputRemapped;
    private Vector2 _virtualShipPosition;
    private Vector2 _virtualReticlePosition;
    private Vector3 _shipWorldVelocity;
    private Vector3 _shipPrevWorldPos;
    private Vector3 _shipWorldDeltaPos;
    public Vector3 ShipVelocity => _shipWorldVelocity;
    public Vector3 ShipDeltaPos => _shipWorldDeltaPos;
    public Vector3 ShipForward => shipTarget.forward;

    private void Start()
    {
        Debug.Assert(reticleTarget.parent == moveTarget, "reticleTarget.parent == cameraTarget");
        Debug.Assert(shipTarget.parent == moveTarget, "shipTarget.parent == cameraTarget");
        reticleTarget.parent = moveTarget;
        shipTarget.parent = moveTarget;
        
        if (Gamepad.current == null)
        {
            Debug.LogError("No gamepad found, please connect one");
            enabled = false;
        }
    }

    private void Update()
    {
        
        _input = Gamepad.current.leftStick.ReadUnprocessedValue();
        _inputRemapped = new Vector2(_input.x + 1 * 0.5f, _input.y + 1 * 0.5f);

        // move the player through world
        moveTarget.position += new Vector3(
            x: _input.x * worldSpeed.x,
            y: _input.y * worldSpeed.y,
            z: worldSpeed.z
        ) * Time.deltaTime;

        // keep player in the tunnel (min/max elevation and strafe)
        moveTarget.position = new Vector3(
            Mathf.Clamp(moveTarget.position.x, minMaxStrafe.x, minMaxStrafe.y),
            Mathf.Clamp(moveTarget.position.y, minMaxElevation.x, minMaxElevation.y),
            moveTarget.position.z
        );

        // make camera roll slightly with inputs
        var moveRotation = Quaternion.Euler(
            _input.y * camMaxPitch,
            _input.x * camMaxYaw,
            _input.x * -camMaxRoll
        );
        moveTarget.localRotation = Quaternion.Slerp(moveRotation, moveTarget.localRotation,
            Mathf.Exp(-1f * cameraRotationSharpness * Time.deltaTime));

        
        // we first calculate movement on normalized virtual positions
        
        if (_input.magnitude < 0.01f)
        {
            // NO INPUT

            // bring normalized reticle position back to center
            if (_virtualReticlePosition.magnitude > deadZone.magnitude)
            {
                _virtualReticlePosition = Vector2.Lerp(Vector2.zero, _virtualReticlePosition,
                    Mathf.Exp(-1f * reticleResponsiveness * Time.deltaTime));
            }

            // bring normalized ship X-position back to center
            if (Mathf.Abs(_virtualShipPosition.x) > deadZone.x)
            {
                _virtualShipPosition = new Vector2(
                    _virtualShipPosition.x + shipMoveSpeed.x * -_virtualReticlePosition.x * Time.deltaTime,
                    _virtualShipPosition.y
                );
            }

            // bring normalized ship Y-position back to center
            if (Mathf.Abs(_virtualShipPosition.y) > deadZone.y)
            {
                _virtualShipPosition = new Vector2(
                    _virtualShipPosition.x,
                    _virtualShipPosition.y + shipMoveSpeed.y * -_virtualReticlePosition.y * Time.deltaTime
                );
            }
        }
        else
        {
            // SOME INPUT
            
            // update normalized reticle position
            _virtualReticlePosition = Vector2.Lerp(_input, _virtualReticlePosition,
                Mathf.Exp(-1f * reticleResponsiveness * Time.deltaTime));

            // update normalized ship position
            _virtualShipPosition = new Vector2(
                _virtualShipPosition.x + shipMoveSpeed.x * _input.x * Time.deltaTime,
                _virtualShipPosition.y + shipMoveSpeed.y * _input.y * Time.deltaTime
            );
        }

        // apply pos-clamping to reticle
        _virtualReticlePosition = new Vector2(
            Mathf.Clamp(_virtualReticlePosition.x, -1f, 1f),
            Mathf.Clamp(_virtualReticlePosition.y, -1f, 1f)
        );

        // apply pos-clamping to ship
        _virtualShipPosition = new Vector2(
            Mathf.Clamp(_virtualShipPosition.x, -1f, 1f),
            Mathf.Clamp(_virtualShipPosition.y, -1f, 1f)
        );

        // move the reticle in local space based on virtual position
        reticleTarget.localPosition = new Vector3(
            _virtualReticlePosition.x * rangeOfReticleMovement.x,
            _virtualReticlePosition.y * rangeOfReticleMovement.y,
            reticleDistance
        );

        // apply amplitude to ship position
        var shipPos = new Vector3(
            _virtualShipPosition.x * rangeOfShipMovement.x,
            _virtualShipPosition.y * rangeOfShipMovement.y,
            shipDistance
        );
        shipTarget.localPosition = shipPos;
        reticleTarget.forward = (reticleTarget.position - shipTarget.position).normalized;
        
        // track some variables about the ship movement to help with shooting
        _shipWorldDeltaPos = shipTarget.position - _shipPrevWorldPos;
        _shipPrevWorldPos = shipTarget.position;
        _shipWorldVelocity = Vector3.Lerp(
            _shipWorldDeltaPos / Time.deltaTime,
            _shipWorldVelocity,
            Mathf.Exp(-1f * 10f * Time.deltaTime)
        );

        // aim the ship at the reticle
        var shipLookAtReticle = Quaternion.LookRotation(reticleTarget.position - shipTarget.position, Vector3.up);
        shipTarget.rotation = shipLookAtReticle;
        
        // add some roll to the ship (not changing aim)
        shipTarget.rotation *= Quaternion.Euler(
            0,
            0,
            _input.x * -Mathf.Clamp(camMaxRoll * 4f, -60f, 60f)
        );
    }
}