import SwiftUI
import SceneKit
import AVFoundation
import Combine
import UIKit

// Standalone iOS preview of the user's independent Unity prototype.
// Not a Unity export; AI replies here are offline scripted, not live model responses.
struct Caller: Codable, Identifiable {
    let id: String
    let displayName: String
    let portrait: String
    let gender: String
    let age: Int
    let ageGroup: String
    let language: String
    let voiceVariant: Int
    let voicePitch: Double
    let voiceRate: Double
    let personality: String
    let occupation: String
    let topic: String
    let greeting: String
    let dialogueHint: String
    let highTrustReply: String
    let mediumTrustReply: String
    let lowTrustReply: String
    let patience: Int
}
struct CallerCollection: Codable { let callers: [Caller] }
struct ConversationLine: Identifiable {
    let id = UUID()
    let content: String
    let player: Bool
}
@MainActor
final class GameState: ObservableObject {
    @Published var callers: [Caller] = []
    @Published var selected: Caller? = nil
    @Published var conversation: [ConversationLine] = []
    @Published var activeCall = false
    @Published var earned = 0
    @Published var trust = 45
    @Published var day = 1
    @Published var timeLeft = 240
    @Published var alertMessage = "مرحبًا بك في أول يوم عمل"
    @Published var replyDraft = ""
    @Published var responses = 0
    private let speaker = AVSpeechSynthesizer()

    init() {
        if let url = Bundle.main.url(forResource: "caller_profiles", withExtension: "json", subdirectory: "Resources"),
           let data = try? Data(contentsOf: url),
           let decoded = try? JSONDecoder().decode(CallerCollection.self, from: data) {
            callers = decoded.callers
        }
    }
    func portrait(for caller: Caller) -> UIImage? {
        let imageName = caller.portrait.components(separatedBy: "/").last ?? ""
        guard let path = Bundle.main.path(forResource: imageName, ofType: "png", inDirectory: "Resources/Portraits") else { return nil }
        return UIImage(contentsOfFile: path)
    }
    func speak(_ message: String, for caller: Caller) {
        speaker.stopSpeaking(at: .immediate)
        let utterance = AVSpeechUtterance(string: message)
        let arabicVoices = AVSpeechSynthesisVoice.speechVoices().filter { $0.language.hasPrefix("ar") }
        if !arabicVoices.isEmpty { utterance.voice = arabicVoices[abs(caller.voiceVariant) % arabicVoices.count] }
        else { utterance.voice = AVSpeechSynthesisVoice(language: "ar-SA") }
        utterance.pitchMultiplier = Float(max(0.7, min(1.6, caller.voicePitch)))
        utterance.rate = Float(max(0.38, min(0.57, caller.voiceRate)))
        speaker.speak(utterance)
    }
    func beginCall(with person: Caller? = nil) {
        guard let candidate = person ?? callers.randomElement() else { return }
        selected = candidate
        activeCall = true
        trust = 45
        responses = 0
        conversation = [ConversationLine(content: candidate.greeting, player: false)]
        alertMessage = "مكالمة مع \(candidate.displayName)"
        speak(candidate.greeting, for: candidate)
    }
    func submit() {
        guard activeCall, let person = selected else { return }
        let message = replyDraft.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !message.isEmpty else { return }
        replyDraft = ""
        conversation.append(ConversationLine(content: message, player: true))
        responses += 1
        if message.contains("ساعد") || message.contains("أفهم") || message.contains("حل") || message.contains("معك") {
            trust = min(100, trust + 23)
        } else if message.contains("لا أعرف") || message.contains("اسكت") {
            trust = max(0, trust - 30)
        } else {
            trust = min(100, trust + 7)
        }
        let answer = trust >= 65 ? person.highTrustReply : (trust >= 30 ? person.mediumTrustReply : person.lowTrustReply)
        conversation.append(ConversationLine(content: answer, player: false))
        speak(answer, for: person)
        if trust < 15 { endCall(success: false) }
        else if responses >= 3 && trust >= 65 { endCall(success: true) }
    }
    func endCall(success: Bool) {
        guard activeCall else { return }
        activeCall = false
        if success {
            earned += 120
            alertMessage = "اكتملت المكالمة! حصلت على 120 نقطة"
        } else {
            alertMessage = "انتهت المكالمة"
        }
    }
    func tick() {
        guard timeLeft > 0 else { return }
        timeLeft -= 1
        if timeLeft == 0 {
            endCall(success: false)
            day += 1
            timeLeft = 240
            alertMessage = "بدأ يوم عمل جديد"
        }
    }
}

private let dark = Color(red: 0.06, green: 0.075, blue: 0.12)
private let tile = Color(red: 0.12, green: 0.15, blue: 0.22)
private let mint = Color(red: 0.33, green: 0.92, blue: 0.77)

struct ContentView: View {
    @EnvironmentObject private var game: GameState
    @State private var tab = 0
    @State private var showAbout = false
    private let ticker = Timer.publish(every: 1, on: .main, in: .common).autoconnect()
    var body: some View {
        ZStack {
            dark.ignoresSafeArea()
            VStack(spacing: 0) {
                HStack(spacing: 15) {
                    Text("CALL CENTER CHAOS").font(.system(size: 19, weight: .black, design: .rounded)).foregroundColor(mint)
                    Spacer(minLength: 8)
                    stat("اليوم", "\(game.day)")
                    stat("النقاط", "\(game.earned)")
                    stat("الوقت", String(format: "%02d:%02d", game.timeLeft / 60, game.timeLeft % 60))
                    Button { showAbout = true } label: { Image(systemName: "info.circle").foregroundStyle(.white) }
                }.padding(.horizontal, 16).padding(.vertical, 9)
                    .background(Color(red: 0.09, green: 0.12, blue: 0.18))
                HStack(spacing: 0) {
                    section("المكتب", icon: "building.2", index: 0)
                    section("المكالمات", icon: "phone.fill", index: 1)
                    section("الشخصيات", icon: "person.3.fill", index: 2)
                }.padding(.vertical, 6)
                Group {
                    switch tab {
                    case 0: office
                    case 1: phone
                    default: directory
                    }
                }.frame(maxWidth: .infinity, maxHeight: .infinity)
                HStack(spacing: 10) {
                    Image(systemName: "megaphone.fill").foregroundStyle(mint)
                    Text(game.alertMessage).lineLimit(1)
                    Spacer()
                    Text("نموذج تجريبي — الردود غير متصلة بـ AI مباشر").font(.caption2).foregroundStyle(.gray)
                }.font(.caption).foregroundStyle(.white).padding(.horizontal, 12).frame(height: 34)
                    .background(Color(red: 0.10, green: 0.13, blue: 0.19))
            }
        }
        .preferredColorScheme(.dark)
        .environment(\.layoutDirection, .rightToLeft)
        .onReceive(ticker) { _ in game.tick() }
        .alert("حول هذه النسخة", isPresented: $showAbout) {
            Button("حسنًا", role: .cancel) {}
        } message: {
            Text("نسخة iOS أصلية تجريبية مستقلة أُنشئت لإخراج IPA غير موقّع. تحتوي على مكتب 3D و144 عميلًا عربيًا وصوتًا وردودًا محلية. ليست تصدير Unity ولا تتضمن نظام AI المباشر أو أونلاين.")
        }
    }
    private func stat(_ label: String, _ value: String) -> some View {
        HStack(spacing: 4) {
            Text(label).foregroundStyle(.gray)
            Text(value).foregroundStyle(.white).bold()
        }.font(.caption2).lineLimit(1)
    }
    private func section(_ label: String, icon: String, index: Int) -> some View {
        Button { tab = index } label: {
            Label(label, systemImage: icon)
                .font(.system(size: 13, weight: .bold))
                .frame(maxWidth: .infinity)
                .padding(.vertical, 9)
                .background(tab == index ? mint.opacity(0.2) : Color.clear)
                .foregroundStyle(tab == index ? mint : .white)
                .clipShape(RoundedRectangle(cornerRadius: 10))
        }.buttonStyle(.plain)
    }
    private var office: some View {
        ZStack(alignment: .bottom) {
            Office3DView().background(Color(red: 0.12, green: 0.18, blue: 0.25))
            HStack(spacing: 13) {
                Label("اسحب لتحريك الكاميرا", systemImage: "hand.draw").font(.caption2)
                Spacer()
                Button {
                    game.beginCall()
                    tab = 1
                } label: {
                    Label("مكالمة جديدة", systemImage: "phone.arrow.up.right.fill")
                        .font(.headline).padding(.horizontal, 21).padding(.vertical, 12)
                        .background(mint).foregroundStyle(dark).clipShape(Capsule())
                }
            }.padding(12).background(.ultraThinMaterial).clipShape(RoundedRectangle(cornerRadius: 18))
                .padding(12)
        }
    }
    private var phone: some View {
        HStack(spacing: 16) {
            VStack(spacing: 10) {
                if let person = game.selected {
                    if let portrait = game.portrait(for: person) {
                        Image(uiImage: portrait).resizable().scaledToFit()
                            .frame(maxWidth: 200, maxHeight: 180).clipShape(RoundedRectangle(cornerRadius: 17))
                    }
                    Text(person.displayName).font(.title3).bold().foregroundStyle(.white)
                    Text("\(person.age) سنة • \(person.occupation)").font(.caption).foregroundStyle(.gray)
                    Text("الثقة: \(game.trust)%").font(.caption).foregroundStyle(mint)
                    ProgressView(value: Double(game.trust), total: 100).tint(mint)
                    if game.activeCall {
                        Button("إنهاء المكالمة") { game.endCall(success: false) }
                            .foregroundStyle(.red)
                    } else {
                        Button("مكالمة جديدة") { game.beginCall() }
                            .buttonStyle(.borderedProminent).tint(mint)
                    }
                } else {
                    Image(systemName: "phone.fill").font(.system(size: 38)).foregroundStyle(mint)
                    Text("ابدأ مكالمة").font(.title3).foregroundStyle(.white)
                    Button("اتصل الآن") { game.beginCall() }
                        .buttonStyle(.borderedProminent).tint(mint)
                }
            }.padding(14).frame(width: 230).frame(maxHeight: .infinity)
                .background(tile).clipShape(RoundedRectangle(cornerRadius: 18))
            VStack(spacing: 9) {
                ScrollViewReader { proxy in
                    ScrollView {
                        LazyVStack(alignment: .leading, spacing: 9) {
                            ForEach(game.conversation) { line in
                                HStack {
                                    if line.player { Spacer(minLength: 50) }
                                    Text(line.content)
                                        .font(.system(size: 13))
                                        .foregroundStyle(.white)
                                        .padding(11)
                                        .background(line.player ? Color(red: 0.1, green: 0.37, blue: 0.36) : tile)
                                        .clipShape(RoundedRectangle(cornerRadius: 13))
                                    if !line.player { Spacer(minLength: 50) }
                                }.id(line.id)
                            }
                        }.padding(.horizontal, 8)
                    }.onChange(of: game.conversation.count) { _ in
                        if let id = game.conversation.last?.id { withAnimation { proxy.scrollTo(id, anchor: .bottom) } }
                    }
                }
                HStack(spacing: 8) {
                    TextField("اكتب ردك بالعربية...", text: $game.replyDraft, axis: .vertical)
                        .lineLimit(1...2).textFieldStyle(.roundedBorder)
                        .disabled(!game.activeCall)
                    Button { game.submit() } label: {
                        Image(systemName: "paperplane.fill").font(.headline)
                    }.buttonStyle(.borderedProminent).tint(mint)
                        .disabled(!game.activeCall || game.replyDraft.isEmpty)
                }
            }.padding(10).frame(maxWidth: .infinity, maxHeight: .infinity)
                .background(Color(red: 0.09, green: 0.11, blue: 0.17))
                .clipShape(RoundedRectangle(cornerRadius: 18))
        }.padding(12)
    }
    private var directory: some View {
        ScrollView {
            LazyVGrid(columns: Array(repeating: GridItem(.flexible(), spacing: 12), count: 5), spacing: 12) {
                ForEach(game.callers) { person in
                    Button {
                        game.beginCall(with: person)
                        tab = 1
                    } label: {
                        VStack(spacing: 4) {
                            if let portrait = game.portrait(for: person) {
                                Image(uiImage: portrait).resizable().scaledToFill()
                                    .frame(height: 92).frame(maxWidth: .infinity)
                                    .clipped().clipShape(RoundedRectangle(cornerRadius: 11))
                            } else {
                                Image(systemName: "person.fill").frame(height: 92).frame(maxWidth: .infinity)
                            }
                            Text(person.displayName).font(.caption2).bold().lineLimit(1)
                            Text("\(person.age) سنة").font(.caption2).foregroundStyle(.gray)
                        }.foregroundStyle(.white).padding(7).background(tile)
                            .clipShape(RoundedRectangle(cornerRadius: 14))
                    }.buttonStyle(.plain)
                }
            }.padding(14)
        }
    }
}

struct Office3DView: UIViewRepresentable {
    func makeUIView(context: Context) -> SCNView {
        let view = SCNView()
        view.scene = createOfficeScene()
        view.backgroundColor = UIColor(red: 0.12, green: 0.19, blue: 0.26, alpha: 1)
        view.allowsCameraControl = true
        view.autoenablesDefaultLighting = true
        view.antialiasingMode = .multisampling4X
        return view
    }
    func updateUIView(_ uiView: SCNView, context: Context) {}
    private func material(_ color: UIColor) -> SCNMaterial {
        let m = SCNMaterial()
        m.diffuse.contents = color
        m.lightingModel = .physicallyBased
        return m
    }
    private func cube(_ size: SCNVector3, _ position: SCNVector3, _ color: UIColor, parent: SCNNode, round: CGFloat = 0.04) {
        let box = SCNBox(width: CGFloat(size.x), height: CGFloat(size.y), length: CGFloat(size.z), chamferRadius: round)
        box.materials = [material(color)]
        let node = SCNNode(geometry: box)
        node.position = position
        parent.addChildNode(node)
    }
    private func ball(_ radius: CGFloat, _ position: SCNVector3, _ color: UIColor, parent: SCNNode) {
        let sphere = SCNSphere(radius: radius)
        sphere.materials = [material(color)]
        let node = SCNNode(geometry: sphere)
        node.position = position
        parent.addChildNode(node)
    }
    private func createOfficeScene() -> SCNScene {
        let scene = SCNScene()
        let root = scene.rootNode
        cube(SCNVector3(17, 0.25, 13), SCNVector3(0, -0.15, 0),
             UIColor(red: 0.33, green: 0.37, blue: 0.43, alpha: 1), parent: root)
        cube(SCNVector3(17, 4.2, 0.2), SCNVector3(0, 2, -6.5),
             UIColor(red: 0.78, green: 0.83, blue: 0.87, alpha: 1), parent: root)
        cube(SCNVector3(0.2, 4.2, 13), SCNVector3(-8.5, 2, 0),
             UIColor(red: 0.68, green: 0.76, blue: 0.83, alpha: 1), parent: root)
        for i in 0..<9 {
            cube(SCNVector3(0.05, 0.01, 13), SCNVector3(Float(i*2-8), -0.005, 0),
                 UIColor(white: 0.48, alpha: 1), parent: root)
        }
        let shirts: [UIColor] = [.systemTeal, .systemIndigo, .systemOrange, .systemPink, .systemGreen]
        let hairs: [UIColor] = [.brown, .black, .darkGray, .systemBrown, .systemGray]
        for i in 0..<6 {
            let x = Float((i % 3) * 4 - 4)
            let z = Float((i / 3) * 4 - 2.1)
            cube(SCNVector3(2.7, 0.19, 1.35), SCNVector3(x, 0.8, z),
                 UIColor(red: 0.5, green: 0.32, blue: 0.22, alpha: 1), parent: root)
            for dx: Float in [-1.12, 1.12] {
                cube(SCNVector3(0.13, 0.75, 0.13), SCNVector3(x + dx, 0.40, z),
                     UIColor.darkGray, parent: root)
            }
            cube(SCNVector3(0.85, 0.62, 0.1), SCNVector3(x, 1.24, z - 0.30),
                 UIColor(red: 0.03, green: 0.11, blue: 0.15, alpha: 1), parent: root)
            cube(SCNVector3(0.73, 0.48, 0.015), SCNVector3(x, 1.25, z - 0.235),
                 UIColor(red: 0.24, green: 0.84, blue: 0.78, alpha: 1), parent: root)
            cube(SCNVector3(0.8, 0.04, 0.32), SCNVector3(x, 0.92, z + 0.35),
                 UIColor(white: 0.15, alpha: 1), parent: root)
            cube(SCNVector3(0.74, 0.73, 0.45), SCNVector3(x, 0.65, z + 1.03),
                 UIColor.darkGray, parent: root)
            let shirt = i == 5 ? UIColor.systemRed : shirts[i]
            cube(SCNVector3(0.68, 0.75, 0.42), SCNVector3(x, 1.3, z + 1.16),
                 shirt, parent: root, round: 0.2)
            ball(0.27, SCNVector3(x, 1.95, z + 1.16),
                 UIColor(red: 0.91, green: 0.65, blue: 0.48, alpha: 1), parent: root)
            let hair = SCNCapsule(capRadius: 0.275, height: 0.2)
            hair.materials = [material(i == 5 ? .black : hairs[i])]
            let hairNode = SCNNode(geometry: hair)
            hairNode.position = SCNVector3(x, 2.17, z + 1.16)
            root.addChildNode(hairNode)
            for dx: Float in [-0.31, 0.31] {
                ball(0.075, SCNVector3(x + dx, 1.97, z + 1.16), UIColor.black, parent: root)
            }
        }
        cube(SCNVector3(5, 1.0, 0.05), SCNVector3(1, 2.7, -6.33),
             UIColor(red: 0.08, green: 0.2, blue: 0.27, alpha: 1), parent: root)
        let sun = SCNLight()
        sun.type = .omni
        sun.intensity = 1300
        let lightNode = SCNNode()
        lightNode.light = sun
        lightNode.position = SCNVector3(0, 6, 3)
        root.addChildNode(lightNode)
        let camera = SCNCamera()
        camera.fieldOfView = 58
        let camNode = SCNNode()
        camNode.camera = camera
        camNode.position = SCNVector3(7.2, 6.4, 10.4)
        let aim = SCNNode()
        aim.position = SCNVector3(0, 0.9, -0.5)
        root.addChildNode(aim)
        let look = SCNLookAtConstraint(target: aim)
        look.isGimbalLockEnabled = true
        camNode.constraints = [look]
        root.addChildNode(camNode)
        scene.background.contents = UIColor(red: 0.12, green: 0.19, blue: 0.26, alpha: 1)
        return scene
    }
}

@main
struct CallCenterChaosPreviewApp: App {
    @StateObject private var game = GameState()
    var body: some Scene {
        WindowGroup {
            ContentView().environmentObject(game)
        }
    }
}
