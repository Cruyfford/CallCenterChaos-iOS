## الحالة بعد رفع الملف — 1 نوفمبر 2026

- تم رفع `CallCenterChaos-iPhone.zip` بنجاح، وفحصت GitHub Actions الأرشيف و144 صورة و144 ملف تعريف دون أخطاء.
- فشل البناء الأول بسبب `Branch is dirty`، وتم إصلاح المشكلة بتحديد `versioning: None` في `.github/workflows/ios-ipa.yml`.
- [البناء الثاني رقم 2](https://github.com/Cruyfford/CallCenterChaos-iOS/actions/runs/41301499452) فشل بسبب: **Missing Unity License File and no Serial was found**. لا توجد نسخة IPA الآن.

### المطلوب لإكمال بناء Unity

إذا كان لديك ترخيص **Unity Personal المجاني**، فعّله بواسطة Unity Hub على حاسوب مناسب واحصل على ملف `Unity_lic.ulf`، ثم أضف في [GitHub Actions Secrets](https://github.com/Cruyfford/CallCenterChaos-iOS/settings/secrets/actions) هذه القيم: `UNITY_LICENSE` (النص الكامل من ULF)، `UNITY_EMAIL` و`UNITY_PASSWORD` بحسب تعليمات GameCI. إذا لديك Unity Pro، استخدم `UNITY_SERIAL` مع `UNITY_EMAIL` و`UNITY_PASSWORD` بدل ذلك. **لا ترسل الأسرار داخل المحادثة أو مستودع GitHub العام.**

دليل GameCI الرسمي: https://game.ci/docs/github/activation/

بعد إعداد الأسرار، افتح [سجل البناء](https://github.com/Cruyfford/CallCenterChaos-iOS/actions/workflows/ios-ipa.yml)، واضغط **Run workflow**. النجاح غير مضمون قبل اختبار تصدير Unity وتجميع Xcode.


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
