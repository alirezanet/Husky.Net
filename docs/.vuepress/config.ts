import { defineUserConfig } from 'vuepress-vite'
import type { DefaultThemeOptions } from 'vuepress-vite'
import type { ViteBundlerOptions } from '@vuepress/bundler-vite'
import { plugin, themeConfig, head } from './configs'

export default defineUserConfig<DefaultThemeOptions, ViteBundlerOptions>({
   lang: 'en-US',
   title: 'Husky.Net',
   description: 'Git hooks made easy with husky.net task runner',
   bundler: '@vuepress/bundler-vite',
   plugins: plugin,
   themeConfig: themeConfig,
   port: 3000,
   base: '/Husky.Net/',
   head: head,
})
