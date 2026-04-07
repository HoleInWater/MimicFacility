// VoiceCloneClient.h — Async HTTP client for the Chatterbox voice cloning sidecar at localhost:8100.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "VoiceCloneClient.generated.h"

USTRUCT(BlueprintType)
struct FVoiceCloneRequest
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadWrite)
	FString Text;

	UPROPERTY(BlueprintReadWrite)
	FString SpeakerReferenceID;

	UPROPERTY(BlueprintReadWrite)
	float Temperature;

	UPROPERTY(BlueprintReadWrite)
	float ExaggerationFactor;

	FVoiceCloneRequest()
		: Temperature(0.7f)
		, ExaggerationFactor(1.0f)
	{}
};

USTRUCT(BlueprintType)
struct FVoiceCloneResponse
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	TArray<uint8> AudioData;

	UPROPERTY(BlueprintReadOnly)
	int32 SampleRate;

	UPROPERTY(BlueprintReadOnly)
	float DurationSeconds;

	UPROPERTY(BlueprintReadOnly)
	bool bSuccess;

	UPROPERTY(BlueprintReadOnly)
	FString ErrorMessage;

	FVoiceCloneResponse() : SampleRate(0), DurationSeconds(0.0f), bSuccess(false) {}
};

DECLARE_DYNAMIC_DELEGATE_OneParam(FOnVoiceCloneComplete, const FVoiceCloneResponse&, Response);

UCLASS(BlueprintType)
class MIMICFACILITY_API UVoiceCloneClient : public UObject
{
	GENERATED_BODY()

public:
	UVoiceCloneClient();

	UFUNCTION(BlueprintCallable, Category = "Voice")
	void SendCloneRequest(const FVoiceCloneRequest& Request, FOnVoiceCloneComplete OnComplete);

	UFUNCTION(BlueprintCallable, Category = "Voice")
	void SetEndpoint(const FString& Host, int32 Port);

	UFUNCTION(BlueprintPure, Category = "Voice")
	bool IsAvailable() const { return bServerAvailable; }

	UFUNCTION(BlueprintCallable, Category = "Voice")
	void CheckServerHealth();

private:
	void HandleSynthesisResponse(FHttpRequestPtr HttpRequest, FHttpResponsePtr HttpResponse,
		bool bConnectedSuccessfully, FOnVoiceCloneComplete OnComplete);

	void HandleHealthCheck(FHttpRequestPtr HttpRequest, FHttpResponsePtr HttpResponse, bool bConnectedSuccessfully);

	FString BuildRequestJSON(const FVoiceCloneRequest& Request) const;
	FVoiceCloneResponse ParseResponseData(FHttpResponsePtr HttpResponse) const;

	FString ServerHost;
	int32 ServerPort;
	bool bServerAvailable;
};
