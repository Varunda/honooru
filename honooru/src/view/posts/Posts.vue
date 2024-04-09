<template>
    <div>
        <app-menu></app-menu>

        <div>
            <div style="display: grid; grid-template-columns: 300px 1fr; gap: 1rem;">
                <div>
                    <post-search v-model="query" @keyup.enter="performSearch" @do-search="performSearch"></post-search>

                    <div v-if="posts.state == 'loaded'" class="no-underline-links">
                        <div v-for="tag in postTags" :style="{ 'color': '#' + tag.hexColor }">
                            <a :href="'/tag/' + tag.id" :class="{ 'text-success': tag.description, 'text-secondary': !tag.description }">
                                <info-hover :text="tag.description || ''"></info-hover>
                            </a>
                            <a :href="'/posts?q=' + tag.name">
                                <span :style="{ 'color': '#' + tag.hexColor }">
                                    {{tag.name}}
                                </span>
                            </a>
                            <span class="text-muted">
                                ({{tag.uses}})
                            </span>
                        </div>
                    </div>

                </div>

                <div>
                    <post-list :q="search" @search-done="searchDone"></post-list>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import PostSearch from "components/app/PostSearch.vue";
    import PostList from "components/app/PostList.vue";

    import { SearchResults } from "api/PostApi";
    import { ExtendedTag } from "api/TagApi";

    export const Posts = Vue.extend({
        props: {

        },

        data: function() {
            return {
                query: "" as string,
                search: "" as string,

                posts: Loadable.idle() as Loading<SearchResults>
            }
        },

        beforeMount: function(): void {
            const params: URLSearchParams = new URLSearchParams(location.search);
            if (params.has("q")) {
                this.query = params.get("q")!;
            } else {
                this.query = "sort:id_desc";
            }
            this.search = this.query;
        },

        methods: {
            performSearch: function(query: string): void {
                console.log(`performing search [query=${query}]`);
                const params: URLSearchParams = new URLSearchParams();
                params.set("q", query);
                location.href = `/posts?${params.toString()}`;
            },

            searchDone: function(data: Loading<SearchResults>): void {
                console.log("done!");
                this.posts = data;
            }
        },

        computed: {
            postTags: function(): ExtendedTag[] {
                if (this.posts.state != "loaded") {
                    return [];
                }

                return [...this.posts.data.tags].sort((a, b) => {
                    return b.uses - a.uses
                        || a.name.localeCompare(b.name);
                }).slice(0, 30);
            },
        },

        components: {
            InfoHover, ApiError,
            AppMenu,
            PostList,
            PostSearch
        }
    });
    export default Posts;

</script>