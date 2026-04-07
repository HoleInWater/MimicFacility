// MimicFacilityCharacter.cpp — Player character implementation with full FPS movement.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicFacilityCharacter.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/InputComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Gear/GearBase.h"

AMimicFacilityCharacter::AMimicFacilityCharacter()
{
	PrimaryActorTick.bCanEverTick = true;

	BaseLookUpRate = 45.0f;
	BaseTurnRate = 45.0f;

	GetCapsuleComponent()->InitCapsuleSize(42.0f, 96.0f);

	FirstPersonCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FirstPersonCamera"));
	FirstPersonCamera->SetupAttachment(GetCapsuleComponent());
	FirstPersonCamera->SetRelativeLocation(FVector(0.0f, 0.0f, 64.0f));
	FirstPersonCamera->bUsePawnControlRotation = true;

	GetCharacterMovement()->MaxWalkSpeed = 400.0f;
	GetCharacterMovement()->BrakingDecelerationWalking = 1500.0f;
	GetCharacterMovement()->JumpZVelocity = 350.0f;
	GetCharacterMovement()->AirControl = 0.15f;

	bUseControllerRotationPitch = false;
	bUseControllerRotationYaw = true;
	bUseControllerRotationRoll = false;
}

void AMimicFacilityCharacter::BeginPlay()
{
	Super::BeginPlay();
	UE_LOG(LogTemp, Log, TEXT("MimicFacilityCharacter spawned: %s"), *GetName());
}

void AMimicFacilityCharacter::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

void AMimicFacilityCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

	// Axis bindings — movement
	PlayerInputComponent->BindAxis("MoveForward", this, &AMimicFacilityCharacter::MoveForward);
	PlayerInputComponent->BindAxis("MoveRight", this, &AMimicFacilityCharacter::MoveRight);

	// Axis bindings — look
	PlayerInputComponent->BindAxis("LookUp", this, &APawn::AddControllerPitchInput);
	PlayerInputComponent->BindAxis("LookRight", this, &APawn::AddControllerYawInput);

	// Action bindings
	PlayerInputComponent->BindAction("Interact", IE_Pressed, this, &AMimicFacilityCharacter::OnInteract);
	PlayerInputComponent->BindAction("UseGear", IE_Pressed, this, &AMimicFacilityCharacter::OnUseGear);
	PlayerInputComponent->BindAction("ToggleFlashlight", IE_Pressed, this, &AMimicFacilityCharacter::OnToggleFlashlight);
}

void AMimicFacilityCharacter::MoveForward(float Value)
{
	if (Value != 0.0f)
	{
		const FRotator Rotation = Controller->GetControlRotation();
		const FRotator YawRotation(0, Rotation.Yaw, 0);
		const FVector Direction = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::X);
		AddMovementInput(Direction, Value);
	}
}

void AMimicFacilityCharacter::MoveRight(float Value)
{
	if (Value != 0.0f)
	{
		const FRotator Rotation = Controller->GetControlRotation();
		const FRotator YawRotation(0, Rotation.Yaw, 0);
		const FVector Direction = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::Y);
		AddMovementInput(Direction, Value);
	}
}

void AMimicFacilityCharacter::LookUpRate(float Value)
{
	AddControllerPitchInput(Value * BaseLookUpRate * GetWorld()->GetDeltaSeconds());
}

void AMimicFacilityCharacter::LookRightRate(float Value)
{
	AddControllerYawInput(Value * BaseTurnRate * GetWorld()->GetDeltaSeconds());
}

void AMimicFacilityCharacter::OnInteract()
{
	UE_LOG(LogTemp, Log, TEXT("Player interact pressed."));

	// Line trace forward to find interactable actors
	FVector Start = FirstPersonCamera->GetComponentLocation();
	FVector End = Start + FirstPersonCamera->GetForwardVector() * 300.0f;

	FHitResult Hit;
	FCollisionQueryParams Params;
	Params.AddIgnoredActor(this);

	if (GetWorld()->LineTraceSingleByChannel(Hit, Start, End, ECC_Visibility, Params))
	{
		if (AGearBase* Gear = Cast<AGearBase>(Hit.GetActor()))
		{
			EquipGear(Gear);
		}
	}
}

void AMimicFacilityCharacter::OnUseGear()
{
	if (EquippedGear)
	{
		EquippedGear->Activate();
	}
}

void AMimicFacilityCharacter::OnToggleFlashlight()
{
	// Flashlight toggle is handled through the gear system
	if (EquippedGear)
	{
		EquippedGear->Activate();
	}
}

void AMimicFacilityCharacter::EquipGear(AGearBase* Gear)
{
	if (!Gear) return;

	EquippedGear = Gear;
	Gear->OnPickedUp(this);
	UE_LOG(LogTemp, Log, TEXT("Player equipped gear: %s"), *Gear->GetName());
}
