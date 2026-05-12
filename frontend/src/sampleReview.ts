import type { DiffRequest } from './types/review'

export const sampleReviewRequest: DiffRequest = {
  repo: 'NguyenDu2710/nuxt-base',
  src_branch: 'pull/1/head',
  dest_branch: 'pull_request_base',
  files: [
    {
      file_path: 'app.vue',
      diff: `@@ -1,138 +1,28 @@
<template>
  <div class="container">
-    <!-- Top Bar -->
-    <div class="topbar">
-      <div class="topbar-right">
-        <form class="search-input">
-          <input type="text" placeholder="Tìm kiếm" />
-          <button type="submit"><i class="fa fa-search"></i></button>
-        </form>
-        <a href="#" class="login">Đăng nhập</a>
-        <a href="#" class="lang"><span>VN</span></a>
-        <a href="#" class="lang"><span>EN</span></a>
-      </div>
-    </div>
+    <TheNavBar />
     <main class="main-content">
-      ... (removed sections banner, guide, news, tour, map, vn360)
+      <TravelGuideView />
+      <TopNewsView />
+      <TravelTourView />
+      <MapView />
+      <VietNam />
     </main>
-    <footer class="footer">...</footer>
+    <TheFooter />
  </div>
</template>`,
    },
    {
      file_path: 'nuxt.config.ts',
      diff: `@@ -1,15 +1,14 @@
 export default defineNuxtConfig({
-  modules: ['@pinia/nuxt'],
+  modules: ['@pinia/nuxt', '@nuxt/image'],
+  css: ['~/css/main.css'],
 })`,
    },
  ],
}
