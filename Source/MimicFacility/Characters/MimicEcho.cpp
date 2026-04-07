// MimicEcho.cpp — Echo Mimic: replays captured conversations to split groups and erode audio trust.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "MimicEcho.h"
#include "Components/AudioComponent.h"
#include "Kismet/GameplayStatics.h"
#include "GameFramework/Character.h"

AMimicEcho::AMimicEcho()
{
	PrimaryActorTick.bCanEverTick = true;
	bReplicates = true;

	// Echo Mimics are invisible — no mesh, no collision with players
	SetActorHiddenInGame(true);

	EchoAudioComponent = CreateDefaultSubobject<UAudioComponent>(TEXT("EchoAudio"));
	EchoAudioComponent->SetupAttachment(RootComponent);
	EchoAudioComponent->bAutoActivate = false;

	PlaybackInterval = 8.0f;
	TriggerRadius = 1500.0f;
	CurrentPhraseIndex = 0;
	bIsPlaying = false;
}

void AMimicEcho::BeginPlay()
{
	Super::BeginPlay();

	if (HasAuthority())
	{
		// Check for nearby players every 3 seconds
		GetWorldTimerManager().SetTimer(
			ProximityCheckTimer,
			this,
			&AMimicEcho::CheckForNearbyPlayers,
			3.0f,
			true
		);
	}

	UE_LOG(LogTemp, Log, TEXT("EchoMimic spawned at %s with %d phrases"),
		*GetActorLocation().ToString(), StoredPhrases.Num());
}

void AMimicEcho::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

void AMimicEcho::SetConversationData(const TArray<FString>& Phrases, const TArray<FString>& SpeakerIDs)
{
	StoredPhrases = Phrases;
	StoredSpeakers = SpeakerIDs;
	CurrentPhraseIndex = 0;

	UE_LOG(LogTemp, Log, TEXT("EchoMimic — Loaded %d phrases from %d speakers"),
		Phrases.Num(), SpeakerIDs.Num());
}

void AMimicEcho::CheckForNearbyPlayers()
{
	if (bIsPlaying || StoredPhrases.Num() == 0) return;

	TArray<AActor*> Players;
	UGameplayStatics::GetAllActorsOfClass(GetWorld(), ACharacter::StaticClass(), Players);

	for (AActor* Player : Players)
	{
		float Distance = FVector::Dist(GetActorLocation(), Player->GetActorLocation());
		if (Distance <= TriggerRadius)
		{
			StartPlayback();
			return;
		}
	}
}

void AMimicEcho::StartPlayback()
{
	if (bIsPlaying || StoredPhrases.Num() == 0) return;

	bIsPlaying = true;
	CurrentPhraseIndex = 0;
	PlayNextPhrase();

	UE_LOG(LogTemp, Warning, TEXT("EchoMimic — Playback started. Replaying %d phrases."), StoredPhrases.Num());
}

void AMimicEcho::StopPlayback()
{
	bIsPlaying = false;
	GetWorldTimerManager().ClearTimer(PlaybackTimer);
	UE_LOG(LogTemp, Log, TEXT("EchoMimic — Playback stopped."));
}

void AMimicEcho::PlayNextPhrase()
{
	if (!bIsPlaying || CurrentPhraseIndex >= StoredPhrases.Num())
	{
		// Loop back to start after a longer pause
		CurrentPhraseIndex = 0;
		GetWorldTimerManager().SetTimer(PlaybackTimer, this, &AMimicEcho::PlayNextPhrase,
			PlaybackInterval * 3.0f, false);
		return;
	}

	FString Speaker = (CurrentPhraseIndex < StoredSpeakers.Num()) ? StoredSpeakers[CurrentPhraseIndex] : TEXT("Unknown");
	FString Phrase = StoredPhrases[CurrentPhraseIndex];

	// In full implementation: route phrase text to TTS with the speaker's cloned voice
	// For now: log it and trigger the audio component
	UE_LOG(LogTemp, Warning, TEXT("[ECHO] %s: \"%s\""), *Speaker, *Phrase);

	CurrentPhraseIndex++;

	// Schedule next phrase
	float Delay = PlaybackInterval + FMath::FRandRange(-2.0f, 3.0f);
	GetWorldTimerManager().SetTimer(PlaybackTimer, this, &AMimicEcho::PlayNextPhrase, Delay, false);
}
