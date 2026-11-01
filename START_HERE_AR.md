# البدء في بناء Call Center Chaos للآيفون

تم ربط المستودع وإضافة [ملف GitHub Actions](.github/workflows/ios-ipa.yml). **لم ينتج IPA بعد**.

## ما الذي تفعله من الآيفون؟

1. حمّل `CallCenterChaos-iPhone.zip` من المحادثة (حوالي 5.7 ميغابايت).
2. افتح: https://github.com/Cruyfford/CallCenterChaos-iOS/upload/main
3. اختر **Add file → Upload files** وأرفق ملف ZIP كما هو، **دون فك الضغط ودون تغيير الاسم**. اضغط Commit changes.
4. سيحاول GitHub Actions تشغيل **Build iOS IPA from Unity archive** تلقائيًا عند رفع الأرشيف.
5. يجب تجهيز ترخيص Unity باستخدام **Settings → Secrets and variables → Actions**: متغير `UNITY_LICENSE` أو بيانات الترخيص الأخرى المناسبة. لا تضع كلمات مرور أو مفاتيح في المستودع العام.
6. إذا رُفع المشروع قبل إضافة الترخيص، اضغط **Actions → Build iOS IPA from Unity archive → Run workflow** بعد تهيئة الترخيص.
7. إن نجح بناء Unity وXcode، ابحث في التشغيل عن `CallCenterChaos-UNSIGNED-IPA` في Artifacts.

**تنبيه:** هذا ملف IPA غير موقّع؛ لا يمكن تثبيته مباشرة على iPhone العادي قبل التوقيع بشهادة Apple صالحة. ونموذج اللعبة ما زال تجريبيًا ولم يجتز اختبار Unity/Xcode على جهاز حقيقي.

**المستودع حاليًا عام Public.** لجعله خاصًا: Settings → General → Danger Zone → Change repository visibility.

تم اختبار أرشيف المصدر محليًا: 144 صورة و144 ملف شخصية؛ اجتازت اختبارات خادم AI الثلاثة، لكن تشغيل الذكاء الاصطناعي الحقيقي يتطلب إعداد خادم HTTPS ومفتاح API محفوظ على الخادم.
