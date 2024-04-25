<template>
    <div>
        <app-menu></app-menu>

        <div>
            <div style="display: grid; grid-template-columns: 300px 1fr; gap: 1rem;">
                <div>
                    <post-search v-model="query" @keyup.enter="performSearch" @do-search="performSearch"></post-search>

                    <div v-if="posts.state == 'loaded'" class="no-underline-links font-monospace">
                        <div v-for="tag in postTags" :style="{ 'color': '#' + tag.hexColor }">
                            <a :href="'/tag/' + tag.id" :class="{ 'text-success': tag.description, 'text-secondary': !tag.description }">
                                <info-hover :text="tag.description || ''"></info-hover>
                            </a>

                            <span class="text-light">
                                <!-- because this font is monospace, all spaces make this look weird, and white-space-collapse is not in firefox yet -->
                                <a href="javascript:void;" @click="addTag(tag.name)">+</a>
                                <a href="javascript:void;" @click="removeTag(tag.name)">-</a>
                            </span>

                            <a :href="'/posts?q=' + tag.name">
                                <span :style="{ 'color': '#' + tag.hexColor }">
                                    {{tag.name.split("_").join(" ")}}
                                </span>
                            </a>
                            <span class="" style="color: var(--bs-gray-600)">
                                {{tag.uses}}
                            </span>
                        </div>
                    </div>
                </div>

                <div>
                    <post-list :q="search" :limit="limit" :offset="offset" @search-done="searchDone" class="mb-3"></post-list>

                    <posts-page :query="query" :offset="offset" :limit="limit" :post-count="postCount"></posts-page>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";
    import AccountUtil from "util/AccountUtil";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import AlertCollapse from "components/AlertCollapse.vue";
    import PostSearch from "components/app/PostSearch.vue";
    import PostList from "components/app/PostList.vue";

    import PostsPage from "./components/PostsPage.vue";

    import { SearchResults } from "api/PostApi";
    import { ExtendedTag } from "api/TagApi";
    import { UserSetting } from "api/UserSettingApi";

    export const Posts = Vue.extend({
        props: {

        },

        data: function() {
            return {
                query: "" as string,
                search: "" as string,
                limit: 10 as number,
                offset: 0 as number,

                usedTags: new Set as Set<string>,

                posts: Loadable.idle() as Loading<SearchResults>
            }
        },

        beforeMount: function(): void {
            document.title = "Honooru / Posts";

            const params: URLSearchParams = new URLSearchParams(location.search);
            if (params.has("q")) {
                this.query = params.get("q")!;
            } else {
                this.query = "";
            }

            const setting: UserSetting | undefined = AccountUtil.getSetting("postings.count");
            if (setting != null) {
                this.limit = Number.parseInt(setting.value);
                if (Number.isNaN(this.limit)) {
                    console.warn(`failed to parse limit value of ${setting.value} to a valid int, defaulting to 10`);
                    this.limit = 10;
                } else {
                    console.log(`setting limit to ${this.limit}`);
                }
            }

            if (params.has("offset")) {
                this.offset = Number.parseInt(params.get("offset")!);
                if (Number.isNaN(this.offset)) {
                    console.warn(`failed to parse offset value of ${params.get("offset")} to a valid int, defaulting to 0`);
                }
            }

            this.usedTags = new Set(this.query.trim().toLowerCase().split(" "));

            this.search = this.query;
        },

        methods: {
            performSearch: function(query: string): void {
                let limit = 10;
                const setting: UserSetting | undefined = AccountUtil.getSetting("postings.count");
                if (setting != null) {
                    limit = Number.parseInt(setting.value);
                    if (Number.isNaN(limit)) {
                        limit = 10;
                    }
                }

                console.log(`performing search [limit=${limit}] [query=${query}]`);
                const params: URLSearchParams = new URLSearchParams();
                params.set("q", query);
                params.set("limit", limit.toString());
                location.href = `/posts?${params.toString()}`;
            },

            searchDone: function(data: Loading<SearchResults>): void {
                console.log("done!");
                this.posts = data;
            },

            addTag: function(tag: string): void {
                this.performSearch(this.query + " " + tag);
            },

            removeTag: function(tag: string): void {
                // if the tag is in the query, remove it
                if (this.query.indexOf(tag) > -1) {
                    this.query = this.query.replace(tag, " ");
                } else { // otherwise add it to the ignore tag
                    this.query += " -" + tag;
                }

                this.performSearch(this.query);
            }
        },

        computed: {
            postTags: function(): ExtendedTag[] {
                if (this.posts.state != "loaded") {
                    return [];
                }

                return [...this.posts.data.tags].sort((a: ExtendedTag, b: ExtendedTag) => {
                    // ensure that the tags used in the query are included in the tag list first
                    const au: number = this.usedTags.has(a.name) == false ? 0 : 1;
                    const bu: number = this.usedTags.has(b.name) == false ? 0 : 1;

                    return bu - au
                        || b.uses - a.uses
                        || a.name.localeCompare(b.name);
                }).slice(0, 30);
            },

            linkTags: function(): string {
                return encodeURI(Array.from(this.usedTags).join(" "));
            },

            currentPage: function(): number {
                return Math.floor(this.offset / this.limit);
            },

            postCount: function(): number {
                if (this.posts.state != "loaded") {
                    return 0;
                }

                return this.posts.data.postCount;
            }
        },

        components: {
            InfoHover, ApiError, AlertCollapse,
            AppMenu,
            PostList,
            PostSearch,
            PostsPage
        }
    });
    export default Posts;

</script>