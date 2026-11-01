#import <Foundation/Foundation.h>
#import <Speech/Speech.h>
#import <AVFoundation/AVFoundation.h>

// iOS-only speech recognition and speech synthesis for fictional characters.
// Actual character-dialogue AI requires a separately hosted server.
extern "C" void UnitySendMessage(const char *obj, const char *method, const char *msg);

static AVAudioEngine *cccEngine;
static SFSpeechRecognizer *cccRecognizer;
static SFSpeechAudioBufferRecognitionRequest *cccRequest;
static SFSpeechRecognitionTask *cccTask;
static AVSpeechSynthesizer *cccSynth;
static NSString *cccTarget;
static bool cccRecognizing = false;
static int cccVoiceIndex = 0;
static float cccPitch = 1.0f;
static float cccRate = .48f;
static NSInteger cccGender = 0;
static NSString *cccVoiceLocale = @"ar-SA";
static NSString *cccLocale = @"ar-SA";

static void cccStopRecognition(void) {
    if (!cccRecognizing) return;
    cccRecognizing = false;
    if (cccEngine.isRunning) [cccEngine stop];
    [cccEngine.inputNode removeTapOnBus:0];
    [cccRequest endAudio];
    [cccTask cancel];
    cccTask = nil;
    cccRequest = nil;
}

static void cccStartRecognition(void) {
    if (cccRecognizing) cccStopRecognition();
    cccRecognizer = [[SFSpeechRecognizer alloc] initWithLocale:[NSLocale localeWithLocaleIdentifier:cccLocale]];
    if (!cccRecognizer.available) return;
    // Avoid recording the synthesizer's own speech.
    [cccSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    NSError *error = nil;
    AVAudioSession *session = [AVAudioSession sharedInstance];
    [session setCategory:AVAudioSessionCategoryPlayAndRecord mode:AVAudioSessionModeDefault options:AVAudioSessionCategoryOptionDefaultToSpeaker error:&error];
    [session setActive:YES error:&error];
    cccEngine = [[AVAudioEngine alloc] init];
    cccRequest = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
    cccRequest.shouldReportPartialResults = YES;
    AVAudioInputNode *input = cccEngine.inputNode;
    AVAudioFormat *format = [input outputFormatForBus:0];
    [input installTapOnBus:0 bufferSize:1024 format:format block:^(AVAudioPCMBuffer *buffer, AVAudioTime *when) {
        if (cccRequest) [cccRequest appendAudioPCMBuffer:buffer];
    }];
    cccRecognizing = true;
    cccTask = [cccRecognizer recognitionTaskWithRequest:cccRequest resultHandler:^(SFSpeechRecognitionResult *result, NSError *err) {
        if (!cccRecognizing) return;
        if (result && result.isFinal) {
            NSString *text = result.bestTranscription.formattedString ?: @"";
            dispatch_async(dispatch_get_main_queue(), ^{
                if (cccTarget) UnitySendMessage(cccTarget.UTF8String, "OnVoiceRecognized", text.UTF8String);
                cccStopRecognition();
            });
        } else if (err) dispatch_async(dispatch_get_main_queue(), ^{ cccStopRecognition(); });
    }];
    [cccEngine prepare];
    if (![cccEngine startAndReturnError:&error]) { cccStopRecognition(); return; }
    // End the short recognition capture; iOS typically emits a final result.
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(7 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        if (cccRecognizing) [cccRequest endAudio];
    });
}

extern "C" void CCC_StartSpeech(const char *gameObjectName) {
    cccTarget = [NSString stringWithUTF8String:gameObjectName ?: ""];
    [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus status) {
        if (status != SFSpeechRecognizerAuthorizationStatusAuthorized) return;
        [[AVAudioSession sharedInstance] requestRecordPermission:^(BOOL granted) {
            if (granted) dispatch_async(dispatch_get_main_queue(), ^{ cccStartRecognition(); });
        }];
    }];
}
extern "C" void CCC_StopSpeech(void) {
    cccStopRecognition();
    [cccSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
}
extern "C" void CCC_SetLanguage(const char *locale) {
    NSString *v = [NSString stringWithUTF8String:locale ?: "en-US"];
    cccLocale = [v hasPrefix:@"ar"] ? @"ar-SA" : @"en-US";
}
extern "C" void CCC_SetVoice(int voiceIndex, float pitch, float rate, int gender, const char *locale) {
    cccVoiceIndex = MAX(0, voiceIndex);
    cccPitch = MAX(0.65f, MIN(1.5f, pitch));
    cccRate = MAX(0.35f, MIN(.58f, rate));
    cccGender = gender;
    NSString *loc = [NSString stringWithUTF8String:locale ?: "ar-SA"];
    cccVoiceLocale = [loc hasPrefix:@"ar"] ? loc : @"en-US";
}
extern "C" void CCC_Speak(const char *text) {
    if (!cccSynth) cccSynth = [[AVSpeechSynthesizer alloc] init];
    [cccSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    NSString *s = [NSString stringWithUTF8String:text ?: ""];
    if (s.length == 0) return;
    AVSpeechUtterance *u = [AVSpeechUtterance speechUtteranceWithString:s];
    // Arabic voice for Arabic text even when the recognition setting is English.
    NSCharacterSet *arabic = [NSCharacterSet characterSetWithRange:NSMakeRange(0x0600,0x06FF-0x0600+1)];
    BOOL containsArabic = [s rangeOfCharacterFromSet:arabic].location != NSNotFound;
    NSString *language = containsArabic ? @"ar" : [cccVoiceLocale substringToIndex:MIN(2,cccVoiceLocale.length)];
    // Choose voice GENDER first, then use the caller's stable variation index. iOS
    // may have only one Arabic voice for a gender; pitch and rate remain distinct.
    NSMutableArray<AVSpeechSynthesisVoice *> *matchingVoices = [NSMutableArray array];
    NSMutableArray<AVSpeechSynthesisVoice *> *genderedVoices = [NSMutableArray array];
    for (AVSpeechSynthesisVoice *v in [AVSpeechSynthesisVoice speechVoices]) {
        if ([v.language hasPrefix:language]) {
            [matchingVoices addObject:v];
            if ((cccGender == 2 && v.gender == AVSpeechSynthesisVoiceGenderFemale) ||
                (cccGender == 1 && v.gender == AVSpeechSynthesisVoiceGenderMale) ||
                cccGender == 0) {
                [genderedVoices addObject:v];
            }
        }
    }
    NSArray<AVSpeechSynthesisVoice *> *choices = genderedVoices.count ? genderedVoices : matchingVoices;
    if (choices.count) u.voice = choices[cccVoiceIndex % choices.count];
    else u.voice = [AVSpeechSynthesisVoice voiceWithLanguage:containsArabic ? @"ar-SA" : @"en-US"];
    u.pitchMultiplier = cccPitch;
    u.rate = cccRate;
    [cccSynth speakUtterance:u];
}
