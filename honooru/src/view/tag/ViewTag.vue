﻿
<template>
    <div>
        <app-menu class="flex-grow-1">
            <menu-dropdown></menu-dropdown>

            <menu-sep></menu-sep>

            <li class="nav-item h1 p-0">
                Posts
            </li>
        </app-menu>

        <hr class="border" />

        <div v-if="tag.state == 'idle'"></div>

        <div v-else-if="tag.state == 'loading'">
            loading...
        </div>

        <div v-else-if="tag.state == 'loaded'">

            <div style="display: grid; grid-template-columns: 300px 1fr; gap: 0.5rem;">
                <div class="border-right mr-3">
                    controls

                    <div class="mr-3">
                        <button class="btn btn-primary d-block w-100" @click="recountPostUsage">
                            re-count post usage
                        </button>
                    </div>
                </div>

                <div>
                    <div>
                        <span :style="{ 'color': '#' + tag.data.hexColor }" class="h2 d-inline">
                            {{tag.data.name}}
                        </span>

                        <span>
                            used in <b>{{tag.data.uses | locale(0)}}</b> posts
                        </span>
                    </div>

                    <div class="mb-3">
                        <button v-if="editing == false" class="btn btn-primary" @click="editing = true">
                            edit
                        </button>

                        <button v-if="editing == true" class="btn btn-success" @click="saveEdit">
                            save
                        </button>

                        <button v-if="editing == true" class="btn btn-secondary" @click="cancelEdit">
                            cancel
                        </button>
                    </div>

                    <div class="mb-2">
                        <label class="mb-0"><strong>name</strong></label>
                        <input v-if="editing" class="form-control" v-model="tagCopy.name" />
                        <input v-else disabled readonly class="form-control-plaintext" v-model="tagCopy.name" />
                    </div>

                    <div class="mb-2">
                        <label class="mb-0"><strong>type</strong></label>
                        <input v-if="!editing"  disabled readonly class="form-control-plaintext" v-model="tagCopy.typeName" />

                        <template v-else>
                            <div v-if="tagTypes.state == 'loading'">
                                loading...
                            </div>

                            <div v-else-if="tagTypes.state == 'loaded'">
                                <select class="form-control" v-model="tagCopy.typeID">
                                    <option v-for="type in tagTypes.data" :value="type.id">
                                        {{type.name}}
                                    </option>
                                </select>
                            </div>
                        </template>
                    </div>

                    <div class="mb-2">
                        <label class="mb-0">description</label>
                        <textarea :disabled="!editing" :readonly="!editing" class="form-control" v-model="tagCopy.description"></textarea>
                    </div>

                    <div v-if="editing">
                        <button class="btn btn-success" @click="saveEdit">
                            save
                        </button>
                    </div>

                    <hr class="border" />

                    <h2>
                        <a :href="'/posts?q=' + tag.data.name">
                            recent posts
                        </a>
                    </h2>

                    <post-list :q="tag.data.name + ' sort:id_desc'" :limit="10"></post-list>
                </div>
            </div>

        </div>

        <div v-else-if="tag.state == 'error'">
            failed to load tag
            <api-error :error="tag.problem"></api-error>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import { AppMenu, MenuSep, MenuDropdown, MenuImage } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import ToggleButton from "components/ToggleButton";
    import PostList from "components/app/PostList.vue";

    import "filters/LocaleFilter";

    import { Post, PostApi } from "api/PostApi";
    import { Tag, ExtendedTag, TagApi } from "api/TagApi";
    import { PostTagApi } from "api/PostTagApi";
    import { TagTypeApi, TagType } from "api/TagTypeApi";

    export const ViewTag = Vue.extend({
        props: {

        },

        data: function() {
            return {
                tagID: 0 as number,
                tag: Loadable.idle() as Loading<ExtendedTag>,
                tagCopy: new ExtendedTag() as ExtendedTag,

                editing: false as boolean,

                tagTypes: Loadable.idle() as Loading<TagType[]>
            }
        },

        created: function(): void {
            this.getTagID();
            this.getTag();

            this.getTagTypes();
        },

        methods: {
            getTagID: function(): void {
                const parts: string[] = location.pathname.split("/");
                console.log(parts);
                if (parts.length < 3) {
                    console.error(`missing tag ID`);
                    return;
                }

                this.tagID = Number.parseInt(parts[2]);

                if (Number.isNaN(this.tagID)) {
                    throw `failed to parse ${parts[2]} to a valid int`;
                }
            },

            getTag: async function(): Promise<void> {
                this.tag = Loadable.loading();
                this.tag = await TagApi.getExtendedByID(this.tagID);

                if (this.tag.state == "loaded") {
                    this.tagCopy = { ...this.tag.data };
                }
            },

            getTagTypes: async function(): Promise<void> {
                this.tagTypes = Loadable.loading();
                this.tagTypes = await TagTypeApi.getAll();
            },

            saveEdit: async function(): Promise<void> {
                this.editing = false;

                const r: Loading<Tag> = await TagApi.update(this.tagID, this.tagCopy);

                if (r.state == "loaded") {
                    Toaster.add("updated tag", "successfully updated tag", "success");
                    await this.getTag();
                } else if (r.state == "error") {
                    Toaster.add("error!", `failed to update tag: ${r.problem.detail}`, "danger");
                }
            },

            cancelEdit: function(): void {
                this.editing = false;

                if (this.tag.state == "loaded") {
                    this.tagCopy = { ...this.tag.data };
                }
            },

            recountPostUsage: async function(): Promise<void> {
                Toaster.add(`update queued`, "queued an updated to the tag usage count", "info");
                await TagApi.queueRecount(this.tagID);
            }

        },

        components: {
            InfoHover, ApiError,
            AppMenu, MenuSep, MenuDropdown, MenuImage,
            ToggleButton, PostList
        }
    });
    export default ViewTag;

</script>