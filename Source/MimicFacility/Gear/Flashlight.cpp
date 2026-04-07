// Flashlight.cpp — Flashlight implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "Flashlight.h"
#include "Components/SpotLightComponent.h"
#include "Net/UnrealNetwork.h"

AFlashlight::AFlashlight()
{
	bIsConsumable = false;
	bIsOn = false;
	GearName = FText::FromString(TEXT("Flashlight"));

	SpotLight = CreateDefaultSubobject<USpotLightComponent>(TEXT("SpotLight"));
	SpotLight->SetupAttachment(RootComponent);
	SpotLight->SetVisibility(false);
	SpotLight->SetIntensity(8000.0f);
	SpotLight->SetOuterConeAngle(35.0f);
	SpotLight->SetInnerConeAngle(25.0f);
	SpotLight->SetAttenuationRadius(2000.0f);
}

void AFlashlight::Activate()
{
	bIsOn = !bIsOn;
	SpotLight->SetVisibility(bIsOn);
	UE_LOG(LogTemp, Log, TEXT("Flashlight %s"), bIsOn ? TEXT("ON") : TEXT("OFF"));
}

void AFlashlight::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	DOREPLIFETIME(AFlashlight, bIsOn);
}
