# بناء اللعبة على الآيفون فقط — Unity Build Automation

هذه الطريقة تستخدم **Unity Cloud Build Automation** بدلًا من GitHub GameCI، حتى لا تحتاج إلى تثبيت Unity Hub على الآيفون أو استخراج ملف Unity Personal ULF محليًا.

**جهزت الملفات بالفعل:** نجح GitHub Actions في فك ضغط ZIP تلقائيًا، وأصبحت مجلدات `Assets` و`Packages` و`ProjectSettings` موجودة مباشرة في فرع `main`. كما حدّثت المشروع إلى Unity `2022.3.62f3` لأن `2022.3.62f1` لم يعد متاحًا في Unity Build Automation اعتبارًا من أكتوبر 2026.

## من Safari على الآيفون

1. افتح https://cloud.unity.com وسجل الدخول بنفس Unity ID. يمكنك اختيار **طلب موقع سطح المكتب** في Safari إذا ظهرت واجهة ناقصة.
2. من Unity Dashboard اختر **Development → Products → DevOps**. أنشئ مشروعًا باسم `CallCenterChaos-iOS` وفعّل الخطة المجانية إن عُرضت.
3. افتح **Build Automation → Settings → Source Control**، واختر **GitHub**. إذا طلب منك رمز وصول GitHub، أنشئ Fine-grained PAT خاصًا بمستودع `Cruyfford/CallCenterChaos-iOS` مع صلاحية `Contents: Read-only` فقط (و`Webhooks: Read and write` إذا فعلت Auto-build)، ثم أدخله **داخل Unity فقط**. لا ترسله في الدردشة ولا تحفظه في مستودع عام.
4. اربط المستودع `https://github.com/Cruyfford/CallCenterChaos-iOS`، والفرع `main`. اترك `Project subfolder path` فارغًا لأن ملفات Unity موجودة في الجذر.
5. افتح **Build Automation → Configurations → Create configuration** واختر منصة **iOS**، Unity `2022.3.62f3`، وmacOS builder، ثم Xcode متوافقًا. اجعل `Target name` مثل `ios-prototype`.
6. من **Advanced Settings → Script hooks → Pre-Export Method** ضع النص حرفيًا: `CCCCloudBuild.PreExport`. هذا ضروري لأنه ينشئ مشهد اللعبة تلقائيًا قبل البناء، إذ إن نسخة المشروع الحالية هي Prototype برمجي.
7. في **Credentials** سيُطلب توقيع iOS. لتثبيت التطبيق على هاتف عادي تحتاج شهادة Apple صالحة `.p12` وملف `.mobileprovision` مرتبطين بتطبيقك وجهازك، أو مسار نشر Apple ملائم. حساب Apple Developer المدفوع يتيح مسارات التوزيع وTestFlight. لا تشارك الشهادات أو مفاتيحها في GitHub العام أو في الدردشة.
8. احفظ الإعدادات واضغط **Build**، وافحص **Build History**. لا يوجد IPA جاهز إلا إذا نجح بناء Unity/Xcode والتوقيع. شارك رابط تشغيل البناء أو لقطة الخطأ لإصلاحه.

## الروابط الرسمية

- Unity Cloud: https://cloud.unity.com
- إعداد DevOps: https://docs.unity.com/en-us/devops/get-started-end-users
- ربط GitHub: https://docs.unity.com/en-us/build-automation/get-started-with-build-automation/connect-your-version-control-system
- إعداد iOS: https://docs.unity.com/en-us/build-automation/basic-build-configuration/overview
- Pre-export method: https://docs.unity.com/en-us/build-automation/advanced-build-configuration/run-custom-scripts-during-the-build-process
- توقيع iOS: https://docs.unity.com/en-us/build-automation/sign-build-artifacts/sign-an-ios-application

**تنبيه:** Unity Build Automation يدير ترخيص الـ Editor في خدمة السحابة، لكن هذا لا يلغي متطلبات توقيع Apple لتثبيت IPA. الخدمة يمكن أن تكون مجانية ضمن حصتها، وقد تفرض رسومًا إضافية عند تجاوزها. لم يتحقق بعد نجاح أي بناء فعلي، ولم يتم إنتاج IPA.

